import unittest
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from model import Skill, simulate_run, summarize

class ModelTests(unittest.TestCase):
    def test_deterministic(self):
        a=simulate_run(12345, Skill.EXPERT, 'territory', 'territory_4').to_dict()
        b=simulate_run(12345, Skill.EXPERT, 'territory', 'territory_4').to_dict()
        self.assertEqual(a,b)

    def test_summary_bounds(self):
        rows=[simulate_run(5000+i, Skill.INTERMEDIATE, 'attack', 'attack_6') for i in range(50)]
        s=summarize(rows)
        for key,value in s.items():
            if key.endswith('_pct'):
                self.assertGreaterEqual(value,0)
                self.assertLessEqual(value,100)

if __name__=='__main__':
    unittest.main()
