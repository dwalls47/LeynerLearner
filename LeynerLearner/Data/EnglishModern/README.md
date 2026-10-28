# English: JPS 1917 Modern

The English in this folder is **JPS 1917 Modern**: the JPS 1917 translation in `Data\English` (*The Holy Scriptures
According to the Masoretic Text*, public domain) with its old-fashioned English put into modern English by
`Tools\EnglishModernizer`. It is made by Leyner Learner; it is not a publication of the Jewish Publication Society.

- Source: `Data\English` (see its README.md), in the same Hebrew verse numbering, one verse per line as
  `chapter:verse text`, UTF-8
- License: **CC0 1.0** (public domain dedication), like the public-domain text it is made from
- Guide: many rules follow the choices in Adam Cohn's *Modernized Tanakh - Based on JPS 1917* (Genesis and Jonah,
  on Sefaria, CC-BY: <https://www.sefaria.org>, <http://modernizedtanakh.blogspot.com>). His text was compared with
  JPS 1917 to learn which words he changes and how; none of his wording is copied.

## What changes

`Tools\EnglishModernizer` applies rules, in this order, then the hand edits, and writes `changes.txt` (every
change with its count, the forms that may be left, sample verses) and `needs-polish.txt` (verses still flagged)
in its folder:

1. **Phrases**: forasmuch as → since; wherefore → why (in a question) or therefore; whence → from where;
   I pray thee → please; came to pass → happened; called his name → named him; in the midst of → in the middle of;
   did eat → ate; here am I → here I am; to-day → today; she bare a son → bore; behold and lo dropped inside a
   sentence ("and, behold, he is" → "and he is") or "Look," where they open one; brought forth out of → out of.
2. **Pronouns**: thou, thee, ye → you; thy → your; thyself → yourself; thine/mine → your/my before a word ("thine
   eyes", "mine own") and yours/mine standing alone. Capitals for G-d are kept (Thou → You, Thy → Your).
   The "thou"/"ye" of a command is dropped: "be thou strong" → "be strong", "Comfort ye" → "Comfort".
3. **Verbs**: the "thou" forms (art → are, hast → have, didst → did, shalt → shall, wilt → will, knowest → know);
   the -eth forms (cometh → comes, sitteth → sits, dieth → dies, dwelleth → lives); shall → will, except when
   addressed to "you" ("You shall not murder") and "shall I/we?" → "should I/we?".
4. **Words**: unto → to, upon → on, forth → out, hath → has, saith → says, spake → spoke, thereof → of it,
   peradventure → perhaps, howbeit → however, brethren → brothers, kine → cows, slew → killed, smote → struck,
   corn → grain, raiment → clothes, asses → donkeys, damsel → young woman, firmament → expanse, and others; British
   -our spellings become American (neighbour → neighbor).

5. **Sentence forms**: old negatives get "do": "fear not" → "do not fear", "he knows not" → "he does not know",
   "knew not" → "did not know", "Came it not?" → "Did it not come?", "has commanded not" → "has not commanded";
   emphatic do/did is dropped: "G-d did prove Abraham" → "G-d proved Abraham"; the old perfect of verbs of motion:
   "Joab is fled" → "Joab has fled", "I am come" → "I have come"; reflexive commands: "Make thee an ark" →
   "Make yourself an ark", "Get thee unto the land" → "Go to the land", "get you up" → "get up". An irregular-verb
   table and the text's own vocabulary give the verb forms.
6. **Hand edits**: about 170 verses the rules cannot handle well ("did compass it round about" → "encircled it",
   "parts not the hoof" → "does not part the hoof") are edited by hand. Each edit is a line of
   `Tools\EnglishModernizer\polish-edits.tsv` (verse, phrase, replacement); `build_polish.py` turns them into
   `polish.json`, which the modernizer applies only where the rules still give the text the edit was made from.

The divine name and "G-d" are as in `Data\English`. Everything is made by rules and these listed hand edits: no
AI service or other paid tool is involved, and the result is the same every time.

## Limits

Sentence order is kept, so some verses stay formal where the English of 1917 is still understood ("Thus shall you
do six days"). `needs-polish.txt` lists the verses the checks still flag (about 80, nearly all acceptable modern
English such as "do righteousness and justice"), and `changes.txt` lists every change with its count.

To rebuild after `Data\English` changes:

    cd Tools\EnglishModernizer
    dotnet run

then run Tools\DataManifest.
