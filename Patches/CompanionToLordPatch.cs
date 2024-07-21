using HarmonyLib;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace MorePrisonerInteractions.Patches
{
    [HarmonyPatch(typeof(CompanionRolesCampaignBehavior), "ClanNameSelectionIsDone")]
    internal class CompanionToLordPatch
    {
        // Token: 0x0600001F RID: 31 RVA: 0x00003080 File Offset: 0x00001280
        public static void Postfix(string clanName)
        {
            Hero hero = Hero.OneToOneConversationHero;
            bool HasSpouse = hero.Spouse != null;
            if (HasSpouse)
            {
                HandleFamily(hero);
            }


        }

        static void HandleFamily(Hero hero)
        {
            if (hero.Spouse.Clan != hero.Clan)
            {
                hero.Spouse.Clan = hero.Clan;
                if (hero.Spouse.CompanionOf == Hero.MainHero.Clan)
                {
                    hero.Spouse.CompanionOf = null;
                    hero.Spouse.SetNewOccupation(Occupation.Lord);
                }
            }


            if (MobileParty.MainParty.MemberRoster.Contains(hero.Spouse.CharacterObject))
            {
                MobileParty.MainParty.MemberRoster.RemoveTroop(hero.Spouse.CharacterObject, 1, default(UniqueTroopDescriptor), 0);
            }
            if (hero.PartyBelongedTo != null && hero.Spouse.PartyBelongedTo != hero.PartyBelongedTo)
            {
                AddHeroToPartyAction.Apply(hero.Spouse, hero.PartyBelongedTo, true);
            }

            HandleChildren(hero);
        }

        static void HandleChildren(Hero hero)
        {
            bool HasChildren = hero.Children.Count > 0;
            if (HasChildren)
            {
                foreach (var child in hero.Children)
                {
                    if (child.Clan != hero.Clan)
                    {
                        HandleChildren(child);
                    }
                }
            }
        }
    }
}
