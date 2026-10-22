using System;
using System.Linq;
using NUnit.Framework;

namespace CozyFishing.Tests
{
    public class FishDataTests
    {
        [Test]
        public void Loads30SpeciesWithUniqueIds()
        {
            Assert.AreEqual(30, FishDatabase.All.Count);
            Assert.AreEqual(30, FishDatabase.All.Select(s => s.id).Distinct().Count());
        }

        [TestCase("paddy"), TestCase("falls"), TestCase("forest"), TestCase("beach"), TestCase("sea")]
        public void EachStageHasSixSpeciesAndOneRare(string stage)
        {
            var l = FishDatabase.ForStage(stage).ToList();
            Assert.AreEqual(6, l.Count);
            Assert.AreEqual(1, l.Count(s => s.rare));
        }

        [Test]
        public void SpeciesValuesArePlayable()
        {
            foreach (var s in FishDatabase.All)
            {
                Assert.Greater(s.price, 0, s.id);
                Assert.Greater(s.kgMax, s.kgMin, s.id);
                Assert.That(s.pull, Is.InRange(0.01f, 0.99f), s.id);
                Assert.That(s.win, Is.GreaterThanOrEqualTo(0.7f), s.id);
                Assert.AreEqual(3, s.shape.Length, s.id);
            }
        }
    }

    public class FishRollerTests
    {
        [Test]
        public void RollsOnlyStageFishWithinWeightRange()
        {
            var rng = new Random(1);
            for (int i = 0; i < 3000; i++)
            {
                var c = FishRoller.Roll("paddy", false, 1, 0, 12, rng);
                Assert.IsFalse(c.IsJunk);
                Assert.AreEqual("paddy", c.species.stage);
                Assert.That(c.kg, Is.InRange(c.species.kgMin, c.species.kgMax));
            }
        }

        [Test]
        public void JunkChanceIsRespected()
        {
            var rng = new Random(2);
            int junk = Enumerable.Range(0, 4000).Count(_ => FishRoller.Roll("paddy", false, 1, 0.1f, 12, rng).IsJunk);
            Assert.That(junk, Is.InRange(300, 520));
        }

        [Test]
        public void RareFishMoreLikelyNearSpecialSpot()
        {
            var rng = new Random(3);
            int normal = Enumerable.Range(0, 4000).Count(_ => FishRoller.Roll("paddy", false, 1, 0, 12, rng).species.rare);
            int near = Enumerable.Range(0, 4000).Count(_ => FishRoller.Roll("paddy", true, 1, 0, 12, rng).species.rare);
            Assert.Greater(near, normal * 3);
        }

        [Test]
        public void SwampEelBitesMoreAtNight()
        {
            var rng = new Random(4);
            int night = Enumerable.Range(0, 3000).Count(_ => FishRoller.Roll("paddy", false, 1, 0, 20, rng).species.id == "lai");
            int day = Enumerable.Range(0, 3000).Count(_ => FishRoller.Roll("paddy", false, 1, 0, 12, rng).species.id == "lai");
            Assert.Greater(night, day * 2);
        }
    }

    public class FightTests
    {
        static FightResult Play(string id, Func<FightModel, bool> policy, int seed = 5)
        {
            var f = new FightModel(FishDatabase.Get(id), false, new Random(seed));
            for (int i = 0; i < 90 * 30; i++)
            {
                var r = f.Step(1f / 30, policy(f));
                if (r != FightResult.Ongoing) return r;
            }
            return FightResult.Ongoing;
        }

        [Test]
        public void HoldingForeverSnapsTheLine() => Assert.AreEqual(FightResult.Snapped, Play("mo", _ => true));

        [Test]
        public void NeverReelingLetsTheFishGo() => Assert.AreNotEqual(FightResult.Caught, Play("mo", _ => false));

        [TestCase("kradi"), TestCase("chado"), TestCase("butong"), TestCase("krathong")]
        public void KeepingTensionInZoneCatchesEveryFish(string id) =>
            Assert.AreEqual(FightResult.Caught, Play(id, f => f.Tension < f.ZoneCenter));

        [Test]
        public void RareFishHaveNarrowerZone()
        {
            var easy = new FightModel(FishDatabase.Get("mo"), false, new Random(1));
            var legend = new FightModel(FishDatabase.Get("butong"), false, new Random(1));
            easy.Step(0.1f, false); legend.Step(0.1f, false);
            Assert.Less(legend.ZoneWidth, easy.ZoneWidth);
        }
    }

    public class StrikeTests
    {
        [Test]
        public void LegendaryNeedsThreeHitsRareNeedsTwo()
        {
            Assert.AreEqual(3, new StrikeModel(4, new Random(1)).Need);
            Assert.AreEqual(2, new StrikeModel(3, new Random(1)).Need);
        }

        [Test]
        public void TwoMissesFail()
        {
            var s = new StrikeModel(4, new Random(1));
            s.Pos = s.Center > 0.5f ? 0 : 1; s.Press();
            s.Pos = s.Center > 0.5f ? 0 : 1; s.Press();
            Assert.IsTrue(s.Failed);
        }

        [Test]
        public void HittingTheZoneSucceeds()
        {
            var s = new StrikeModel(3, new Random(1));
            for (int i = 0; i < s.Need; i++) { s.Pos = s.Center; Assert.IsTrue(s.Press()); }
            Assert.IsTrue(s.Done);
        }
    
        [Test]
        public void Dex_RecordsNewSpeciesCountAndBestWeight()
        {
            FishDex.ResetForTests();
            Assert.IsTrue(FishDex.Record("test_fish", 0.5f));
            Assert.IsFalse(FishDex.Record("test_fish", 0.3f));
            Assert.AreEqual(2, FishDex.Count("test_fish"));
            Assert.AreEqual(0.5f, FishDex.BestKg("test_fish"), 1e-4);
            Assert.IsTrue(FishDex.Has("test_fish"));
            FishDex.ResetForTests();
        }
    }
}
