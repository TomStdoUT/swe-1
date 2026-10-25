# Polling & Opinion Prediction Platform

An interactive polling application where users create custom polls, stake points on community predictions, discuss choices, and earn rewards based on majority consensus.

---

# Nariai

* Tomas Verseckas (Group leader)
* Tomas Saulius 
* Justinas Makauskas
* Matas Mikalauskas

---

## Key Features

* **Poll Creation & Tagging:** Set custom voting time limits with automated poll closing. Categorize polls by topics (Sports, Tech, Politics).
* **Voting, Points & Staking:** Stake points to double your balance upon guessing the majority. Earn early-bird bonus points for early participation.
* **Daily Challenges:** System-generated daily featured polls offering double point rewards.
* **Discussion Section:** Dedicated comment threads under every poll to justify choices.
* **User Following:** Follow top-performing users to view and copy their prediction picks.
* **Profiles & Rewards:** Track public accuracy rates (%) and redeem points for custom avatars and colored usernames.

---

## Setup & Installation

<b>1. Clone the repository:</b>
<pre>git clone https://github.com/TomStdoUT/swe-1.git</pre>

<b>2. Navigate to the project folder:</b>
<pre>cd swe-1</pre>

<b>3. Check repository status:</b>
<pre>git status</pre>

## Backend database

The backend uses Entity Framework Core with SQLite. On startup, EF Core applies the checked-in migrations to `backend/swe-1.db`. If the legacy `backend/users.json` and `backend/poll_snapshot.json` files exist and the database is empty, the backend imports their records once. The JSON files are left untouched as a backup and are no longer used for storage.

To run the backend from the repository root:

<pre>dotnet run --project backend/swe-1.csproj</pre>

The SQLite file is local and ignored by Git. Future schema changes should be added as EF Core migrations using the local tool manifest:

<pre>dotnet tool restore
dotnet tool run dotnet-ef -- migrations add MigrationName --project backend/swe-1.csproj --startup-project backend/swe-1.csproj --output-dir Data/Migrations</pre>
