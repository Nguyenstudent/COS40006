"""Assert-based tests for validate(). They do not need Redis.

Run:  python test_app.py          (or inside the image:  docker run --rm <image> python test_app.py)
"""
from app import validate


def check(form, expected_errors):
    errors, _ = validate(form)
    assert len(errors) == expected_errors, (form, errors)


check({"unit": "SWE40006", "minutes": "90", "note": "Docker lab"}, 0)
check({"unit": "swe40006", "minutes": "1"}, 0)            # lower case is accepted and upper-cased
check({"unit": "SWE40006", "minutes": "600"}, 0)          # upper limit
check({"unit": "ABC", "minutes": "0"}, 2)                 # bad unit code and too few minutes
check({"unit": "SWE4000", "minutes": "30"}, 1)            # only 4 digits
check({"unit": "SWE40006", "minutes": "601"}, 1)          # too many minutes
check({"unit": "SWE40006", "minutes": "1.5"}, 1)          # not a whole number
check({"unit": "SWE40006", "minutes": "abc"}, 1)
check({"unit": "SWE40006", "minutes": "30", "note": "x" * 101}, 1)
check({}, 2)                                              # empty form

_, values = validate({"unit": " cos10009 ", "minutes": " 45 ", "note": " <script>alert(1)</script> "})
assert values == {"unit": "COS10009", "minutes": 45, "note": "<script>alert(1)</script>"}, values

print("All validate() tests passed.")
