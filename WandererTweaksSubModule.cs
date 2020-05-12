#define RETIRE_BY_KILLING

using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Actions;

using ReflectionExtension;

namespace WandererTweaksModule {
    internal class WandererTweaksBehavior: CampaignBehaviorBase {
        private const int wanderersToSpawn = 2;
        private UrbanCharactersCampaignBehavior urbanCharsBhv;
#if !RETIRE_BY_KILLING
        private List<Hero> urbanCharsBhvCompanions;
#endif
        private List<CharacterObject> urbanCharsBhvCompanionTemplates;
        private List<Hero> heroSeenBhvHeroesToCheck;
        private int availableWandererCount;
        private int targetWandererCount;

        private bool ShallSpawnMore => Clan.PlayerClan.Tier >= 2;

        private bool ShallRetire => availableWandererCount >= targetWandererCount;

        public override void RegisterEvents() {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
#if DEBUG
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
#endif
        }

        public override void SyncData(IDataStore dataStore) {
            // NOP
        }

        private void OnHeroCreated(Hero hero, bool isNatural) {
            if (hero.IsWanderer) {
                availableWandererCount++;
                if (ShallRetire) {
                    CampaignEvents.HeroCreated.ClearListeners(this);
                }
            }
        }

        private void OnClanTierIncreased(Clan clan, bool shouldNotify) {
            if (ShallSpawnMore) {
                IncreaseSpawnFrequency();
                CampaignEvents.ClanTierIncrease.ClearListeners(this);
            }
        }

        private void IncreaseSpawnFrequency() {
            urbanCharsBhv.SetFieldValue("_randomCompanionSpawnFrequencyInWeeks", 0f);
        }

        private void OnSessionLaunched(CampaignGameStarter gameStarter) {
            // We want to "cache" private fields to avoid reflection in the "hotter" paths, but in this case it is important to understand
            // at which stage those fields are initialized. RegisterEvents() often is too early for that.
            // Many fields are inited in SyncData(), some (like _companionTemplates used here) - in some other event handler.
            // In this particular case OnSessionLaunched() seems to be a sweet spot in the late initialization phase where everything we
            // require is already in place.
            urbanCharsBhv = Campaign.Current.GetCampaignBehavior<UrbanCharactersCampaignBehavior>();
            urbanCharsBhvCompanionTemplates = urbanCharsBhv.GetFieldValue<List<CharacterObject>>("_companionTemplates");
#if !RETIRE_BY_KILLING
            urbanCharsBhvCompanions = urbanCharsBhv.GetFieldValue<List<Hero>>("_companions");
#endif
            var heroSeenBhv = Campaign.Current.GetCampaignBehavior<HeroLastSeenUpdaterCampaignBehavior>();
            heroSeenBhvHeroesToCheck = heroSeenBhv.GetFieldValue<List<Hero>>("_heroesToCheck");

            // increase pool of wanderers to increase diversity of spawned templates before retirement hits
            var urbanCharsBhvTargetCompanionNumber = urbanCharsBhv.GetFieldValue<float>("TargetCompanionNumber");
            targetWandererCount = urbanCharsBhvCompanionTemplates.Count + (int)urbanCharsBhvTargetCompanionNumber;
            availableWandererCount = Hero.All.Count(hero => HeroIsAvailableWanderer(hero));
            if (!ShallRetire) {
                CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            }

            // settlement is eligible to spawning a companion after 2 weeks (which is more like 2 months in-game scale)
            urbanCharsBhv.SetFieldValue("_companionSpawnCooldownForSettlementInWeeks", 2f);

            // time to spawn some companions every week?
            if (ShallSpawnMore) {
                IncreaseSpawnFrequency();
            } else {
                CampaignEvents.ClanTierIncrease.AddNonSerializedListener(this, OnClanTierIncreased);
            }
        }

#if DEBUG
        private void DailyTick() {
            var numWanderers = Hero.All.Count(hero => hero.IsWanderer);
            var numWanderesAlive = Hero.All.Count(hero => hero.IsWanderer && hero.IsAlive);
            var numWanderersAvail = Hero.All.Count(hero => HeroIsAvailableWanderer(hero));
            InformationManager.DisplayMessage(new InformationMessage($"{GetType().Name}: numWanderersAvail/Alive/Ttl={numWanderersAvail}/{numWanderesAlive}/{numWanderers}", TaleWorlds.Library.Colors.Magenta));
        }
#endif

        private void WeeklyTick() {
            if (ShallSpawnMore) {
                SpawnWanderers();
            }
            if (ShallRetire) {
                RetireWanderers();
            }
        }

        private void SpawnWanderers() {
            for (int i = 0; i < wanderersToSpawn - 1; i++) { // one is spawned by UrbanCharactersCampaignBehavior.WeeklyTick()
                var companionTemplate = urbanCharsBhvCompanionTemplates.GetRandomElement();
                urbanCharsBhv.InvokeMethod("CreateCompanion", companionTemplate);
            }
        }

        private void RetireWanderers() { // TODO ideally this should be outright "delete", but a safe one, of course
            // heroes are naturally ordered by creation time
            // take first wanderer who is not already "retired", and who is not currently accompanying us and do the job
            foreach (var hero in Hero.All.Where(hero => HeroIsAvailableWanderer(hero))
                                         .Take(wanderersToSpawn)) {
#if !RETIRE_BY_KILLING
                hero.ChangeState(Hero.CharacterStates.Disabled);
#else
                // KillCharacterAction -> MakeDead() will change state
                // LeaveSettlementAction is still required because it removes a guy from the actual tavern
#endif
                // apparently a wanderer may get so unlucky (recall that a random wanderer with culture preference is settled only when
                // MainHero enters an eligible town) that she's still in limbo by the time of retirement
                if (hero.StayingInSettlementOfNotable != null) {
                    LeaveSettlementAction.ApplyForCharacterOnly(hero);
                }
#if !RETIRE_BY_KILLING
                var urbanRemoved = urbanCharsBhvCompanions.Remove(hero);
                InformationManager.DisplayMessage(new InformationMessage($"\"Retired\" wanderer {hero} (urbanRemoved={urbanRemoved})"));
#else
                KillCharacterAction.ApplyByRemove(hero); // UrbanCharactersCampaignBehavior.OnHeroKilled() will Remove() from urbanCharsBhvCompanions
                // superfluous since Native v1.2.0:
                //InformationManager.DisplayMessage(new InformationMessage($"\"Retired\" wanderer {hero}"));
#endif
            }
        }

        // fix that misleading "last seen at" for NotSpawned wanderers
        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero enteredHero) {
            if (enteredHero != null && enteredHero.IsHumanPlayerCharacter) { // apparently may be called w/ enteredHero==null
                // HeroLastSeenUpdaterCampaignBehavior.OnSettlementEntered() down the invoke chain will SyncLastSeenInformation() for all
                // alive heroes (HeroLastSeenUpdaterCampaignBehavior._heroesToCheck).
                // And HeroLastSeenUpdaterCampaignBehavior.OnTick() updates cachedLastSeenInformation for all heroes (in chunks).
                // For not-spawned wanderers it will cache HomeSettlement with IsNearbySettlement: false (the misleading "last seen *at*").
                // Our task, then, is to override cached information with IsNearbySettlement: true.
                // Yes, "last seen *near* XXX" is a bit subtle, but it is safer and faster than e.g. assigning null thru reflection.
                foreach (var hero in heroSeenBhvHeroesToCheck.Where(hero => hero.IsWanderer && hero.IsNotSpawned)) {
                    hero.CacheLastSeenInformation(hero.LastSeenPlace ?? hero.HomeSettlement, IsNearbySettlement: true);
                    // hero.SyncLastSeenInformation(); // see above
                }
            }
        }

        private static bool HeroIsAvailableWanderer(Hero hero) {
            return hero.IsWanderer && hero.CompanionOf is null && !hero.IsDisabled && !hero.IsDead;
        }
    }

    public class WandererTweaksSubModule: MBSubModuleBase {
        protected override void OnGameStart(Game game, IGameStarter gameStarter) {
            if (game.GameType is Campaign) {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;
                campaignStarter.AddBehavior(new WandererTweaksBehavior());
            }
        }
    }
}
