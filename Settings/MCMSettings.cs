using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace MoreHeroInteractions.Settings
{
    internal class MCMSettings : AttributeGlobalSettings<MCMSettings>
    {
        public override string Id => "MorePrisonerInteractions";

        public override string DisplayName => "More Prisoner Interactions";

        public override string FolderName => "MorePrisonerInteractions";

        public override string FormatType => "xml";


        // Demand Ransom
        [SettingPropertyInteger("{=Settings_MPI_RansomMinDays}Minimum Days For Ransom To Arrive", minValue: 0, maxValue: 100, Order = 1, HintText = "{=Settings_MPI_RansomMinDaysDesc}The minimum number of days for the ransom denars to arrive after prisoner writes a letter.", RequireRestart = false)]
        [SettingPropertyGroup("{=Settings_MPI_DemandRansom}Demand Ransom", GroupOrder = 2)]
        public int demandRansomMinDaysToArrive { get; set; } = 1;
        [SettingPropertyInteger("{=Settings_MPI_RansomMaxDays}Maximum Days For Ransom To Arrive", minValue: 0, maxValue: 100, Order = 2, HintText = "{=Settings_MPI_RansomMaxDaysDesc}The maximum number of days for the ransom denars to arrive after prisoner writes a letter.", RequireRestart = false)]
        [SettingPropertyGroup("{=Settings_MPI_DemandRansom}Demand Ransom", GroupOrder = 2)]
        public int demandRansomMaxDaysToArrive { get; set; } = 3;
        [SettingPropertyInteger("{=Settings_MPI_ChanceToPayRansom}Chance For Clan Members To Send Ransom", minValue: 0, maxValue: 100, Order = 3, HintText = "{=Settings_MPI_ChanceToPayRansomDesc}After the prisoner writes a letter to his/her clan members, this is the chance that the clan members will send the ransom amount.", RequireRestart = false)]
        [SettingPropertyGroup("{=Settings_MPI_DemandRansom}Demand Ransom", GroupOrder = 2)]
        public int demandRansomChanceOfRansomArriving { get; set; } = 3;
        [SettingPropertyInteger("{=Settings_MPI_RansomGivenPrisonerDeadMinRelationLoss}Ransom Given But Prisoner Dead Minimum Relation Loss", minValue: 0, maxValue: 100, Order = 4, HintText = "{=Settings_MPI_RansomGivenPrisonerDeadMinRelationLossDesc}After the ransom arrives, but the prisoner is dead. The prisoner's clan members will not be happy. How much should the minimum relation loss with the clan members be?", RequireRestart = false)]
        [SettingPropertyGroup("{=Settings_MPI_DemandRansom}Demand Ransom", GroupOrder = 2)]
        public int demandRansomPrisonerDeadRansomGivenMinRelationLoss { get; set; } = 10;
        [SettingPropertyInteger("{=Settings_MPI_RansomGivenPrisonerDeadMaxRelationLoss}Ransom Given But Prisoner Dead Maximum Relation Loss", minValue: 0, maxValue: 100, Order = 5, HintText = "{=Settings_MPI_RansomGivenPrisonerDeadMaxRelationLossDesc}After the ransom arrives, but the prisoner is dead. The prisoner's clan members will not be happy. How much should the maximum relation loss with the clan members be?", RequireRestart = false)]
        [SettingPropertyGroup("{=Settings_MPI_DemandRansom}Demand Ransom", GroupOrder = 2)]
        public int demandRansomPrisonerDeadRansomGivenMaxRelationLoss { get; set; } = 20;

        // Strip lord
        [SettingPropertyInteger("{=Settings_MPI_StripMinRelationLoss}Minimum Relation Loss From Strip", minValue: 0, maxValue: 100, Order = 1, HintText = "{=Settings_MPI_StripMinRelationLossDesc}The minimum amount of relations that will be lost with the lord that you stripped.", RequireRestart = false)]
        [SettingPropertyGroup("{=Settings_MPI_Strip}Strip Lords", GroupOrder = 3)]
        public int stripPrisonerLordMinRelationLoss { get; set; } = 1;
        [SettingPropertyInteger("{=Settings_MPI_StripMaxRelationLoss}Maximum Relation Loss From Strip", minValue: 0, maxValue: 100, Order = 2, HintText = "{=Settings_MPI_StripMaxRelationLossDesc}The maximum amount of relations that will be lost with the lord that you stripped.", RequireRestart = false)]
        [SettingPropertyGroup("{=Settings_MPI_Strip}Strip Lords", GroupOrder = 3)]
        public int stripPrisonerLordMaxRelationLoss { get; set; } = 3;



    }
}
