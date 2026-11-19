# Submission day: 24 Nov 2026

ELIA submits Shift+Shift to GitHub Game Off 2026 on 24 Nov (official close: 2 Dec 2026, 03:07 IST). Do the steps in
this order: the history step rewrites every commit, so it must come before the tag and the submission.

## 1. Sign the history (on the PC, Unity closed)

```
cd C:\dev\shift-shift
git fetch origin
git reset --hard origin/main
git -c alias.signhistory='!sh tools/sign-history.sh' signhistory
```

Check the printed totals (every commit signed, only ELIA as author), then:

```
git push --force-with-lease origin main
```

On GitHub, the commit list should show "Verified" on every commit. If there is a `mobile` branch, rebase it onto the
signed main afterwards (Docs/MOBILE-KICKOFF.md, "After the jam signing step").

## 2. Tag the submitted version

```
git tag -s v1.0-jam -m "Shift+Shift - GitHub Game Off 2026 submission"
git push origin v1.0-jam
```

(`-s` signs the tag with the same key; check with `git tag -v v1.0-jam`.)

Optional: a GitHub release from the tag, so the repo page shows the jam version: github.com/ELIAJNA/Shift-Shift >
Releases > Draft a new release > choose `v1.0-jam` > title "Shift+Shift v1.0 (Game Off 2026)" > paste the itch.io
link and the controls > attach the WebGL zip > Publish release.

## 3. Make the itch.io page public and submit

1. itch.io > Edit game > Visibility & access: **Public** > Save. Open the page logged out (private window) to check.
2. Open the jam page (itch.io/jam/game-off-2026) > **Submit your project** > choose Shift+Shift > answer the
   submission questions if there are any (the GitHub repo link: https://github.com/ELIAJNA/Shift-Shift) > Submit.
3. Check the entry appears under the jam's Submissions / Entries.

## 4. Community post

On the jam page, open the **Community** tab, and post in the thread for sharing games (or a new topic if there is
none). Text, ready to paste:

> **Shift+Shift: a rhythm runner played with just two keys**
>
> You've pressed Shift a million times. Never like this.
>
> Left Shift and Right Shift are the only controls, and they shift into seven moves: tap to change lane, hold left to
> slide, hold right to sprint, both to jump, hold both to phase between two layers of the world, and left-right-left-
> right to charge through brick walls. Every obstacle lands on the beat, across six levels from 120 to 190 BPM.
>
> Play in your browser: https://jna24.itch.io/shift-shift
> Source: https://github.com/ELIAJNA/Shift-Shift
> Trailer: (YouTube link)
>
> I'd love feedback on the timing and on which level got you. I'll play and rate entries posted here, so drop yours
> below.

Also share the link on social media with #GameOff2026 if you use it.

## 5. After submitting

- Tell the cloud session: it resets its copy to the signed main, ticks the checklist and logs the submission.
- During voting (until 1 Jan 2027): rate at least 20 other entries with written comments; fix only game-breaking bugs
  and note any change on the page.
