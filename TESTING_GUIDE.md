# 🧪 Stripe Payment Integration - Complete Testing Guide

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [Initial Setup](#initial-setup)
3. [Testing Steps](#testing-steps)
4. [Troubleshooting](#troubleshooting)
5. [Production Deployment](#production-deployment)

---

## ✅ Prerequisites

Before testing, make sure you have:

- [x] Stripe account (free test account)
- [x] GitHub account
- [x] GitHub Organization created
- [x] Private repository in GitHub Organization
- [x] GitHub Personal Access Token

---

## 🔧 Initial Setup

### Step 1: Create Stripe Test Account

1. Go to: https://dashboard.stripe.com/register
2. Sign up for a free Stripe account
3. You'll automatically be in "Test Mode" (look for "Test Mode" badge in top-right)

### Step 2: Get Stripe API Keys

1. In Stripe Dashboard, click "Developers" → "API keys"
2. Copy the following keys:
   - **Publishable key** (starts with `pk_test_...`)
   - **Secret key** (starts with `sk_test_...`) - Click "Reveal test key"

### Step 3: Set Up Stripe Webhook

1. In Stripe Dashboard, click "Developers" → "Webhooks"
2. Click "Add endpoint"
3. Enter endpoint URL: `https://codenex.live/api/payment/webhook` (or `http://localhost:7150/api/payment/webhook` for local testing)
4. Select events to listen to:
   - `checkout.session.completed`
5. Click "Add endpoint"
6. Copy the "Signing secret" (starts with `whsec_...`)

**For Local Testing:**
- Use Stripe CLI to forward webhooks: https://stripe.com/docs/stripe-cli
- Run: `stripe listen --forward-to localhost:7150/api/payment/webhook`

### Step 4: Create GitHub Organization

1. Go to GitHub → Top-right profile → "Your organizations"
2. Click "New organization"
3. Choose "Free" plan
4. Name it (e.g., "CodeNex-Premium")
5. Complete setup

### Step 5: Create Private Repository in Organization

1. Go to your organization on GitHub
2. Click "New repository"
3. Name it (e.g., "test-premium-repo")
4. Select "Private"
5. Click "Create repository"

### Step 6: Generate GitHub Personal Access Token

1. Go to GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token" → "Generate new token (classic)"
3. Name: "CodeNex-Premium-Access"
4. Select scopes:
   - ✅ `repo` (Full control of private repositories)
   - ✅ `admin:org` (Full control of organizations)
5. Click "Generate token"
6. **IMPORTANT:** Copy the token immediately (starts with `ghp_...`) - You won't see it again!

### Step 7: Update .env File

Open `.env` file and update these values:

```env
# Stripe Configuration
STRIPE_SECRET_KEY=sk_test_YOUR_ACTUAL_SECRET_KEY_HERE
STRIPE_PUBLISHABLE_KEY=pk_test_YOUR_ACTUAL_PUBLISHABLE_KEY_HERE
STRIPE_WEBHOOK_SECRET=whsec_YOUR_ACTUAL_WEBHOOK_SECRET_HERE

# GitHub Configuration
GITHUB_PERSONAL_ACCESS_TOKEN=ghp_YOUR_ACTUAL_TOKEN_HERE
GITHUB_ORGANIZATION_NAME=CodeNex-Premium  # Or your org name
```

### Step 8: Apply Database Migration

Run the migration to update your database:

```bash
dotnet ef database update --project CodeNex.csproj
```

---

## 🧪 Testing Steps

### Phase 1: Test Free Repository

1. **Start the application:**
   ```bash
   dotnet run --project CodeNex.csproj
   ```

2. **Navigate to:** `http://localhost:7150/Repository.html`

3. **Verify free repository behavior:**
   - Free repos should have "View on GitHub" button
   - Clicking should open the public GitHub URL

---

### Phase 2: Add Premium Repository (Admin Panel)

1. **Log in as Admin:**
   - Navigate to: `http://localhost:7150/Auth.html`
   - Email: `admin@codenex.live`
   - Password: `Admin@456`

2. **Go to Admin Panel:**
   - Navigate to: `http://localhost:7150/Admin.html`
   - Click "Repository" section

3. **Add Premium Repository:**
   - Click "Add Repository"
   - Fill in:
     - **Title:** Test Premium CRM
     - **Description:** Premium repository for testing
     - **Category:** Web
     - **Select:** Premium (checkbox)
     - **Price:** 29.99
     - **GitHub Repo Full Name:** CodeNex-Premium/test-premium-repo  
       _(Format: OrganizationName/RepositoryName)_
     - **Tags:** test, premium
     - **Technical Stack:** React, Node.js
   - Click "Save"

4. **Verify it appears in Repository page**

---

### Phase 3: Test Purchase Flow

1. **Log out of admin account**

2. **Create a test user account:**
   - Navigate to: `http://localhost:7150/Auth.html`
   - Click "Register"
   - Create a new account
   - Verify email (check console logs for verification link)

3. **Go to Repository page:**
   - Navigate to: `http://localhost:7150/Repository.html`
   - Find the premium repository

4. **Verify premium repository displays:**
   - 👑 Premium badge
   - 💰 Price ($29.99)
   - 🔒 "Purchase Now" button

5. **Click "Purchase Now":**
   - Modal should open asking for GitHub username
   - Enter your real GitHub username (important!)
   - Click "Proceed to Payment"

6. **Stripe Checkout opens:**
   - Use Stripe test card: `4242 4242 4242 4242`
   - Expiry: Any future date (e.g., `12/25`)
   - CVC: Any 3 digits (e.g., `123`)
   - ZIP: Any 5 digits (e.g., `12345`)
   - Click "Pay"

7. **Payment processes:**
   - You should be redirected back to Repository page
   - Success message should appear

8. **Verify purchase:**
   - Refresh the page
   - Premium repository should now show:
     - ✅ "Owned" badge
     - "Open on GitHub" button

---

### Phase 4: Verify GitHub Access

1. **Check your email:**
   - You should receive an email from GitHub
   - Subject: "Invitation to collaborate on [organization/repo]"

2. **Accept invitation:**
   - Click "View invitation" in email
   - OR go to: https://github.com/your-org/your-repo
   - Click "Accept invitation"

3. **Verify access:**
   - Go to: `https://github.com/CodeNex-Premium/test-premium-repo`
   - You should now have read access!
   - You can view, clone, and download the repository

---

### Phase 5: Test Webhook (Backend)

1. **Check application logs:**
   - Look for these log messages:
     ```
     Stripe webhook received: checkout.session.completed
     Processing completed checkout session: cs_test_...
     Payment record created: [ID]
     User purchase record created: [ID]
     GitHub access granted to [username] for repository [repo-name]
     ```

2. **Check Stripe Dashboard:**
   - Go to: https://dashboard.stripe.com/test/payments
   - Your test payment should be listed
   - Status should be "Succeeded"

3. **Check database:**
   - Payments table should have new record
   - UserPurchases table should have new record
   - `GitHubAccessGranted` should be `true`

---

## 🐛 Troubleshooting

### Issue: "Stripe Secret Key is not configured"

**Solution:**
- Make sure `.env` file is in the root directory
- Verify `STRIPE_SECRET_KEY` is set correctly
- Restart the application

### Issue: "GitHub Personal Access Token is not configured"

**Solution:**
- Verify `GITHUB_PERSONAL_ACCESS_TOKEN` in `.env`
- Make sure token has correct scopes (`repo`, `admin:org`)
- Token should start with `ghp_`

### Issue: "GitHub username does not exist"

**Solution:**
- Make sure you entered exact GitHub username (case-sensitive)
- Test at: https://github.com/[username]
- Username should exist and be active

### Issue: "Failed to grant GitHub access"

**Possible causes:**
1. **Repository doesn't exist:**
   - Check organization and repo name spelling
   - Format: `OrganizationName/RepositoryName`

2. **Repository is not private:**
   - Make sure repo is set to "Private" in GitHub

3. **Token doesn't have permissions:**
   - Generate new token with `repo` and `admin:org` scopes

4. **Organization name mismatch:**
   - Verify `GITHUB_ORGANIZATION_NAME` in `.env` matches actual org name

### Issue: "Webhook not receiving events"

**For Local Testing:**
- Use Stripe CLI:
  ```bash
  stripe listen --forward-to localhost:7150/api/payment/webhook
  ```
- Update `STRIPE_WEBHOOK_SECRET` with the secret from Stripe CLI

**For Production:**
- Make sure webhook endpoint is accessible publicly
- URL should be: `https://codenex.live/api/payment/webhook`
- Check Stripe Dashboard → Webhooks → [Your endpoint] → "Event deliveries"

### Issue: "Payment succeeded but no GitHub invite"

**Check logs for:**
- "GitHubRepoFullName not set" → Update repository with correct repo name
- "Failed to invite user" → Check GitHub token permissions
- Check database: UserPurchases table, GitHubInviteSent should be true

---

## 🚀 Production Deployment

### Before Going Live:

1. **Switch to Live Stripe Keys:**
   - In Stripe Dashboard, toggle "Test Mode" → "Live Mode"
   - Get live keys from: Developers → API keys
   - Update `.env`:
     ```env
     STRIPE_SECRET_KEY=sk_live_...
     STRIPE_PUBLISHABLE_KEY=pk_live_...
     ```

2. **Update Webhook Endpoint:**
   - In Stripe Dashboard (Live Mode)
   - Add webhook: `https://codenex.live/api/payment/webhook`
   - Copy new webhook secret
   - Update `.env`:
     ```env
     STRIPE_WEBHOOK_SECRET=whsec_...
     ```

3. **Update Success/Cancel URLs:**
   - Already configured in `.env`:
     ```env
     STRIPE_SUCCESS_URL=https://codenex.live/Repository.html?payment=success
     STRIPE_CANCEL_URL=https://codenex.live/Repository.html?payment=cancel
     ```

4. **Test with Real Card (Small Amount):**
   - Create a low-price test repository ($0.50)
   - Purchase with real card
   - Verify entire flow works
   - Refund the test payment in Stripe Dashboard

5. **Enable Stripe Radar (Fraud Protection):**
   - Automatically enabled in live mode
   - Review: https://dashboard.stripe.com/radar/overview

---

## 💳 Stripe Test Cards

For testing different scenarios:

| Card Number | Scenario |
|-------------|----------|
| `4242 4242 4242 4242` | Success |
| `4000 0000 0000 0002` | Card declined |
| `4000 0000 0000 9995` | Insufficient funds |
| `4000 0025 0000 3155` | Requires authentication (3D Secure) |

More test cards: https://stripe.com/docs/testing

---

## 📊 Monitoring in Production

### Stripe Dashboard:
- Monitor: https://dashboard.stripe.com/payments
- Check webhook deliveries
- Review failed payments

### Application Logs:
- Monitor for errors in payment processing
- Check GitHub API responses

### Database:
- Regularly check Payments table for status
- Monitor UserPurchases for access grants

---

## 🎉 Success Checklist

After completing all tests, you should have:

- [x] Free repositories showing GitHub links
- [x] Premium repositories showing price and purchase button
- [x] Payment flow working with Stripe test cards
- [x] GitHub invitations sent automatically after payment
- [x] Users able to access private repos after purchase
- [x] Webhook processing payments correctly
- [x] Database recording all transactions

---

## 📞 Support

If you encounter issues:

1. Check application logs
2. Check Stripe Dashboard → Webhooks → Event deliveries
3. Check GitHub → Settings → Personal access tokens
4. Verify all `.env` values are correct

**Need help?** 
- Stripe Support: https://support.stripe.com
- GitHub Support: https://support.github.com

---

**Last Updated:** 2025-10-08  
**Version:** 1.0
