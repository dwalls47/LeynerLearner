"""Builds polish.json from the hand edits in polish-edits.tsv (book, chapter:verse, phrase, replacement - tab
separated), applied to the rules' output of each verse. Each phrase must occur exactly once in its verse.

To add a hand edit: add a line to polish-edits.tsv, then, in Tools\\EnglishModernizer:

    python build_polish.py polish-edits.tsv ..\\..\\LeynerLearner\\Data polish.json
    dotnet run

(If the rules have changed since polish.json was made, delete polish.json and run `dotnet run` first, so
Data\\EnglishModern holds the rules' output, then build and run again.)"""
import collections
import json
import os
import re
import sys

spec, data, out = sys.argv[1], sys.argv[2], sys.argv[3]
existing = {(e["Book"], e["Ref"]): e for e in json.load(open(out, encoding="utf-8"))} if os.path.exists(out) else {}

texts = {}
for name in os.listdir(os.path.join(data, "EnglishModern")):
    if name.endswith(".txt"):
        for line in open(os.path.join(data, "EnglishModern", name), encoding="utf-8"):
            m = re.match(r"(\d+:\d+) (.*)", line.rstrip("\n"))
            if m:
                texts[(name[:-4], m[1])] = m[2]

edits = collections.OrderedDict()
problems = 0
for n, line in enumerate(open(spec, encoding="utf-8"), 1):
    if not line.strip():
        continue
    book, ref, find, repl = line.rstrip("\n").split("\t")
    key = (book, ref)
    if key not in texts:
        print(f"line {n}: no verse {book} {ref}"); problems += 1; continue
    # If polish.json already holds this verse, start from its original rules text.
    base = existing[key]["From"] if key in existing else texts[key]
    cur = edits.get(key, (base, base))[1]
    count = cur.count(find)
    if count != 1:
        print(f"line {n}: {book} {ref}: '{find}' found {count} times in: {cur}"); problems += 1; continue
    edits[key] = (base, cur.replace(find, repl))

merged = dict(existing)
for (book, ref), (frm, to) in edits.items():
    merged[(book, ref)] = {"Book": book, "Ref": ref, "From": frm, "To": to}


def order(k):
    b, r = k
    c, v = map(int, r.split(":"))
    return (b, c, v)


json.dump([merged[k] for k in sorted(merged, key=order)], open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
print(f"{len(edits)} verses edited ({sum(1 for _ in open(spec, encoding='utf-8') if _.strip())} phrase edits); "
      f"polish.json has {len(merged)} verses; {problems} problems")
