"""Render a bounded recipe and publish a preview into the selected Pixelorama inbox."""
import argparse
from pathlib import Path
from art_jobs import publish

parser = argparse.ArgumentParser()
parser.add_argument("recipe", type=Path)
parser.add_argument("inbox", type=Path)
args = parser.parse_args()
print(publish(args.recipe, args.inbox, args.inbox / "status.json"))
