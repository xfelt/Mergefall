# Google Play Data Safety Form — Answers for Mergefall

Fill Play Console → App content → Data safety using the answers below.
They assume the release build ships with **AdMob + Play Billing + Firebase Analytics/Remote Config**
(the defines `MERGE_SURVIVOR_USE_ADMOB`, `MERGE_SURVIVOR_USE_IAP`, `MERGE_SURVIVOR_USE_FIREBASE`).

> If you ship the first release with the stub services (no SDKs compiled in),
> answer **"No"** to data collection and sharing entirely, and skip the rest.
> Update the form before shipping the SDK-enabled build.

## Overview questions

| Question | Answer |
|---|---|
| Does your app collect or share any of the required user data types? | **Yes** |
| Is all of the user data collected by your app encrypted in transit? | **Yes** |
| Do you provide a way for users to request that their data is deleted? | **Yes** (email request — see PRIVACY_POLICY.md §7) |

## Data types collected

### Device or other IDs
- **Device or other IDs** (Advertising ID, app instance ID)
  - Collected: Yes · Shared: Yes (with Google AdMob/Firebase as service providers)
  - Processed ephemerally: No
  - Required: Yes (ads cannot be served without it; rewarded ads are optional to watch)
  - Purposes: Advertising or marketing; Analytics

### App activity
- **App interactions** (gameplay events: level start/finish, merges, purchases of virtual goods)
  - Collected: Yes · Shared: No
  - Purposes: Analytics; App functionality

### App info and performance
- **Crash logs / Diagnostics** (if Firebase Crashlytics is added; otherwise omit)
  - Collected: Yes · Shared: No
  - Purposes: Analytics (stability)

### Purchases
- **Purchase history** (in-app purchase tokens, processed by Google Play Billing)
  - Collected: Yes · Shared: No
  - Purposes: App functionality

### NOT collected (answer No)
Location, personal info (name/email), financial info (card details — handled entirely by
Google Play), photos/videos, audio, contacts, calendar, messages, health, browsing history.

## Other Play Console declarations

| Section | Answer |
|---|---|
| **Ads** (App content → Ads) | Yes, the app contains ads (rewarded + interstitial, AdMob) |
| **Content rating questionnaire** | Category: Game. No violence against realistic humans (stylized fantasy combat), no sexual content, no profanity, **contains simulated gambling? No** — but **does offer randomized virtual items for purchase (loot box)**: answer the paid-random-items question **Yes** and ensure odds are disclosed in-game (the Lucky Chest screen shows drop rates) |
| **Target audience** | 13+ recommended (avoids Families policy requirements while using AdMob) |
| **News app** | No |
| **COVID-19 tracing** | No |
| **Data deletion** | Provide the privacy-policy URL section on deletion (PRIVACY_POLICY.md §6–7) |
| **Government app** | No |
| **Financial features** | None |
| **Health** | None |
| **Advertising ID** (App content → Advertising ID) | **Yes**, the app uses advertising ID, for: Advertising/marketing, Analytics. The AdMob SDK adds `com.google.android.gms.permission.AD_ID` to the manifest automatically |

## In-app products to create (Play Console → Monetize → Products)

Must match `AppPlatformConfig.CoinPackProductIds` exactly:

| Product ID | Type | Suggested price tier |
|---|---|---|
| `gems_pouch` | Consumable | ~$0.99 |
| `gems_stack` | Consumable | ~$4.99 |
| `gems_chest` | Consumable | ~$9.99 |
| `gems_vault` | Consumable | ~$24.99 |
| `gems_hoard` | Consumable | ~$49.99 |
| `starter_bundle` | Non-consumable | ~$2.99 |
