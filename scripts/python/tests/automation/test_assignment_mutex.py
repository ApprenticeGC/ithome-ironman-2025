#!/usr/bin/env python3
import pathlib
import sys
import unittest
from datetime import datetime, timezone

PRODUCTION_DIR = pathlib.Path(__file__).parent.parent / "production"
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

import rfc_assignment_mutex as ram


class SeriesExtractionTests(unittest.TestCase):
    def test_extract_series(self):
        self.assertEqual(ram.extract_series("Game-RFC-123-04: Task"), "RFC-123")
        self.assertEqual(ram.extract_series("RFC-7-2 something"), "RFC-007")
        self.assertIsNone(ram.extract_series("No series here"))

    def test_extract_series_micro(self):
        self.assertEqual(ram.extract_series_micro("Game-RFC-010-05: Task"), "RFC-010-05")
        self.assertIsNone(ram.extract_series_micro("Random title"))


class SeriesStateTests(unittest.TestCase):
    def setUp(self):
        self.state = ram.SeriesState.default("RFC-001")

    def test_apply_candidate_acquires_when_empty(self):
        status = self.state.apply_candidate(candidate_issue=10, candidate_open=True, active_issue_open=None)
        self.assertEqual(status, "acquired")
        self.assertEqual(self.state.active_issue, 10)
        self.assertEqual(self.state.queue, [])

    def test_apply_candidate_queue_when_active_open(self):
        self.state.active_issue = 11
        status = self.state.apply_candidate(candidate_issue=12, candidate_open=True, active_issue_open=True)
        self.assertEqual(status, "queued")
        self.assertIn(12, self.state.queue)
        self.assertEqual(self.state.active_issue, 11)

    def test_promote_when_active_closed(self):
        self.state.active_issue = 20
        self.state.queue = [21]
        status = self.state.apply_candidate(candidate_issue=30, candidate_open=True, active_issue_open=False)
        self.assertEqual(status, "acquired")
        self.assertEqual(self.state.active_issue, 30)
        self.assertNotIn(30, self.state.queue)

    def test_already_active(self):
        self.state.active_issue = 5
        self.state.queue = [5, 6]
        status = self.state.apply_candidate(candidate_issue=5, candidate_open=True, active_issue_open=True)
        self.assertEqual(status, "already-active")
        self.assertNotIn(5, self.state.queue)

    def test_candidate_closed_raises(self):
        with self.assertRaises(ram.MutexError):
            self.state.apply_candidate(candidate_issue=99, candidate_open=False, active_issue_open=None)


if __name__ == "__main__":
    unittest.main()
