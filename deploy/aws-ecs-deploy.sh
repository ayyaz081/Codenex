#!/bin/bash
# ============================================
# Deploy CodeNex to AWS ECS (Elastic Container Service)
# ============================================

set -e  # Exit on error

# Configuration
AWS_REGION="us-east-1"
CLUSTER_NAME="codenex-cluster"
SERVICE_NAME="codenex-service"
TASK_FAMILY="codenex-task"
ECR_REPO_NAME="codenex"
IMAGE_TAG="latest"
CONTAINER_NAME="codenex"

echo "=========================================="
echo "Deploying CodeNex to AWS ECS"
echo "=========================================="

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    echo "Error: AWS CLI is not installed. Install from https://aws.amazon.com/cli/"
    exit 1
fi

# Check AWS credentials
echo "Checking AWS credentials..."
aws sts get-caller-identity > /dev/null || {
    echo "Error: AWS credentials not configured. Run 'aws configure'"
    exit 1
}

# Get AWS account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPO_NAME}"

echo "AWS Account: $AWS_ACCOUNT_ID"
echo "Region: $AWS_REGION"

# Create ECR repository
echo "Creating ECR repository: $ECR_REPO_NAME..."
aws ecr describe-repositories --repository-names $ECR_REPO_NAME --region $AWS_REGION > /dev/null 2>&1 || \
aws ecr create-repository \
    --repository-name $ECR_REPO_NAME \
    --region $AWS_REGION \
    --image-scanning-configuration scanOnPush=true

# Login to ECR
echo "Logging in to ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_URI

# Build Docker image
echo "Building Docker image..."
docker build -t $ECR_REPO_NAME:$IMAGE_TAG .

# Tag and push image to ECR
echo "Pushing image to ECR..."
docker tag $ECR_REPO_NAME:$IMAGE_TAG $ECR_URI:$IMAGE_TAG
docker push $ECR_URI:$IMAGE_TAG

# Create ECS cluster
echo "Creating ECS cluster: $CLUSTER_NAME..."
aws ecs describe-clusters --clusters $CLUSTER_NAME --region $AWS_REGION > /dev/null 2>&1 || \
aws ecs create-cluster \
    --cluster-name $CLUSTER_NAME \
    --region $AWS_REGION

# Create task execution role (if not exists)
TASK_ROLE_NAME="ecsTaskExecutionRole"
aws iam get-role --role-name $TASK_ROLE_NAME > /dev/null 2>&1 || \
aws iam create-role \
    --role-name $TASK_ROLE_NAME \
    --assume-role-policy-document '{
        "Version": "2012-10-17",
        "Statement": [{
            "Effect": "Allow",
            "Principal": {"Service": "ecs-tasks.amazonaws.com"},
            "Action": "sts:AssumeRole"
        }]
    }' && \
aws iam attach-role-policy \
    --role-name $TASK_ROLE_NAME \
    --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy

TASK_ROLE_ARN=$(aws iam get-role --role-name $TASK_ROLE_NAME --query Role.Arn --output text)

# Register task definition
echo "Registering ECS task definition..."
cat > task-definition.json <<EOF
{
  "family": "$TASK_FAMILY",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "1024",
  "memory": "2048",
  "executionRoleArn": "$TASK_ROLE_ARN",
  "containerDefinitions": [
    {
      "name": "$CONTAINER_NAME",
      "image": "$ECR_URI:$IMAGE_TAG",
      "portMappings": [
        {
          "containerPort": 7150,
          "protocol": "tcp"
        }
      ],
      "essential": true,
      "environment": [
        {"name": "ASPNETCORE_ENVIRONMENT", "value": "Production"},
        {"name": "ASPNETCORE_URLS", "value": "http://+:7150"}
      ],
      "secrets": [
        {"name": "DATABASE_CONNECTION_STRING", "valueFrom": "arn:aws:secretsmanager:${AWS_REGION}:${AWS_ACCOUNT_ID}:secret:codenex/db-connection"},
        {"name": "JWT_KEY", "valueFrom": "arn:aws:secretsmanager:${AWS_REGION}:${AWS_ACCOUNT_ID}:secret:codenex/jwt-key"}
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/$TASK_FAMILY",
          "awslogs-region": "$AWS_REGION",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": ["CMD-SHELL", "curl -f http://localhost:7150/health/live || exit 1"],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      }
    }
  ]
}
EOF

aws ecs register-task-definition \
    --cli-input-json file://task-definition.json \
    --region $AWS_REGION

# Create CloudWatch log group
aws logs create-log-group --log-group-name /ecs/$TASK_FAMILY --region $AWS_REGION 2>/dev/null || true

echo ""
echo "=========================================="
echo "Task definition registered!"
echo "=========================================="
echo ""
echo "IMPORTANT: Complete these steps before creating the service:"
echo ""
echo "1. Create RDS SQL Server instance in AWS Console"
echo "2. Store secrets in AWS Secrets Manager:"
echo "   aws secretsmanager create-secret --name codenex/db-connection --secret-string 'YOUR_CONNECTION_STRING' --region $AWS_REGION"
echo "   aws secretsmanager create-secret --name codenex/jwt-key --secret-string 'YOUR_JWT_KEY' --region $AWS_REGION"
echo ""
echo "3. Create VPC, subnets, and security groups"
echo "4. Create Application Load Balancer (ALB)"
echo ""
echo "5. Create ECS service (after completing above steps):"
echo "   aws ecs create-service \\"
echo "     --cluster $CLUSTER_NAME \\"
echo "     --service-name $SERVICE_NAME \\"
echo "     --task-definition $TASK_FAMILY \\"
echo "     --desired-count 1 \\"
echo "     --launch-type FARGATE \\"
echo "     --network-configuration 'awsvpcConfiguration={subnets=[subnet-xxx],securityGroups=[sg-xxx],assignPublicIp=ENABLED}' \\"
echo "     --load-balancers targetGroupArn=arn:aws:elasticloadbalancing:...,containerName=$CONTAINER_NAME,containerPort=7150 \\"
echo "     --region $AWS_REGION"
echo ""
echo "Clean up temp file..."
rm -f task-definition.json
