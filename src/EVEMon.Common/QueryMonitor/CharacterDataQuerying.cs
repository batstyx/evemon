using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Helpers;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Net;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Service;

namespace EVEMon.Common.QueryMonitor
{
    internal sealed class CharacterDataQuerying
    {
        #region Fields

        private readonly CharacterQueryMonitor<SerializableAPICharacterSheet> m_charSheetMonitor;
        private readonly CharacterQueryMonitor<SerializableAPIMarketOrders> m_charMarketOrdersMonitor;
        private readonly CharacterQueryMonitor<SerializableAPIContracts> m_charContractsMonitor;
        private readonly CharacterQueryMonitor<SerializableAPIIndustryJobs> m_charIndustryJobsMonitor;
        private readonly List<IQueryMonitorEx> m_characterQueryMonitors;
        private readonly List<IQueryMonitorEx> m_basicFeaturesMonitors;
        private readonly CCPCharacter m_ccpCharacter;

        #endregion


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterDataQuerying"/> class.
        /// </summary>
        /// <param name="ccpCharacter">The CCP character.</param>
        internal CharacterDataQuerying(CCPCharacter ccpCharacter)
        {
            // Initializes the query monitors 
            m_ccpCharacter = ccpCharacter;
            m_characterQueryMonitors = new List<IQueryMonitorEx>();

            m_charSheetMonitor = new CharacterQueryMonitor<SerializableAPICharacterSheet>(ccpCharacter,
                CCPAPICharacterMethods.CharacterSheet, OnCharacterSheetUpdated);
            IQueryMonitorEx skillQueueMonitor = new CharacterQueryMonitor<SerializableAPISkillQueue>(ccpCharacter,
                CCPAPICharacterMethods.SkillQueue, OnSkillQueueUpdated);
            IQueryMonitorEx charStandingsMonitor = new CharacterQueryMonitor<SerializableAPIStandings>(ccpCharacter,
                CCPAPICharacterMethods.Standings, OnStandingsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charContactsMonitor = new CharacterQueryMonitor<SerializableAPIContactList>(ccpCharacter,
                CCPAPICharacterMethods.ContactList, OnContactsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charFacWarStatsMonitor = new CharacterQueryMonitor<SerializableAPIFactionalWarfareStats>(ccpCharacter,
                CCPAPICharacterMethods.FactionalWarfareStats, OnFactionalWarfareStatsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charMedalsMonitor = new CharacterQueryMonitor<SerializableAPIMedals>(ccpCharacter,
                CCPAPICharacterMethods.Medals, OnMedalsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charKillLogMonitor = new CharacterQueryMonitor<SerializableAPIKillLog>(ccpCharacter,
                CCPAPICharacterMethods.KillLog, OnKillLogUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charAssetsMonitor = new CharacterQueryMonitor<SerializableAPIAssetList>(ccpCharacter,
                CCPAPICharacterMethods.AssetList, OnAssetsUpdated)
            { QueryOnStartup = true };
            m_charMarketOrdersMonitor = new CharacterQueryMonitor<SerializableAPIMarketOrders>(ccpCharacter,
                CCPAPICharacterMethods.MarketOrders, OnMarketOrdersUpdated)
            { QueryOnStartup = true };
            m_charContractsMonitor = new CharacterQueryMonitor<SerializableAPIContracts>(ccpCharacter,
                CCPAPICharacterMethods.Contracts, OnContractsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charWalletJournalMonitor = new CharacterQueryMonitor<SerializableAPIWalletJournal>(ccpCharacter,
                CCPAPICharacterMethods.WalletJournal, OnWalletJournalUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charWalletTransactionsMonitor = new CharacterQueryMonitor<SerializableAPIWalletTransactions>(ccpCharacter,
                CCPAPICharacterMethods.WalletTransactions, OnWalletTransactionsUpdated)
            { QueryOnStartup = true };
            m_charIndustryJobsMonitor = new CharacterQueryMonitor<SerializableAPIIndustryJobs>(ccpCharacter,
                CCPAPICharacterMethods.IndustryJobs, OnIndustryJobsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charResearchPointsMonitor = new CharacterQueryMonitor<SerializableAPIResearch>(ccpCharacter,
                CCPAPICharacterMethods.ResearchPoints, OnResearchPointsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charEVEMailMessagesMonitor = new CharacterQueryMonitor<SerializableAPIMailMessages>(ccpCharacter,
                CCPAPICharacterMethods.MailMessages, OnEVEMailMessagesUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charEVENotificationsMonitor = new CharacterQueryMonitor<SerializableAPINotifications>(ccpCharacter,
                CCPAPICharacterMethods.Notifications, OnEVENotificationsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charUpcomingCalendarEventsMonitor = new CharacterQueryMonitor<SerializableAPIUpcomingCalendarEvents>(ccpCharacter,
                CCPAPICharacterMethods.UpcomingCalendarEvents, OnUpcomingCalendarEventsUpdated)
            { QueryOnStartup = true };
            IQueryMonitorEx charPlanetaryColoniesMonitor = new CharacterQueryMonitor<SerializableAPIPlanetaryColonies>(ccpCharacter,
                CCPAPIGenericMethods.PlanetaryColonies, OnPlanetaryColoniesUpdated)
            { QueryOnStartup = true };

            // Add the monitors in an order as they will appear in the throbber menu
            m_characterQueryMonitors.AddRange(new[]
            {
                m_charSheetMonitor,
                skillQueueMonitor,
                charStandingsMonitor,
                charContactsMonitor,
                charFacWarStatsMonitor,
                charMedalsMonitor,
                charKillLogMonitor,
                charAssetsMonitor,
                m_charMarketOrdersMonitor,
                m_charContractsMonitor,
                charWalletJournalMonitor,
                charWalletTransactionsMonitor,
                m_charIndustryJobsMonitor,
                charPlanetaryColoniesMonitor,
                charResearchPointsMonitor,
                charEVEMailMessagesMonitor,
                charEVENotificationsMonitor,
                charUpcomingCalendarEventsMonitor
            });

            m_characterQueryMonitors.ForEach(monitor => ccpCharacter.QueryMonitors.Add(monitor));

            m_basicFeaturesMonitors = m_characterQueryMonitors
                .Select(monitor =>
                    new
                    {
                        monitor,
                        method = (CCPAPICharacterMethods)monitor.Method
                    })
                .Where(monitor =>
                    (long)monitor.method == ((long)monitor.method & (long)CCPAPIMethodsEnum.BasicCharacterFeatures))
                .Select(basicFeature => basicFeature.monitor)
                .ToList();

            if (ccpCharacter.ForceUpdateBasicFeatures)
                m_basicFeaturesMonitors.ForEach(monitor => monitor.ForceUpdate(true));

            EveMonClient.TimerTick += EveMonClient_TimerTick;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets the character sheet monitor.
        /// </summary>
        /// <value>The character sheet monitor.</value>
        internal CharacterQueryMonitor<SerializableAPICharacterSheet> CharacterSheetMonitor => m_charSheetMonitor;

        /// <summary>
        /// Gets or sets a value indicating whether the character market orders have been queried.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the character market orders have been queried; otherwise, <c>false</c>.
        /// </value>
        internal bool CharacterMarketOrdersQueried => !m_charMarketOrdersMonitor.IsUpdating;

        /// <summary>
        /// Gets or sets a value indicating whether the character contracts have been queried.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the character contracts have been queried; otherwise, <c>false</c>.
        /// </value>
        internal bool CharacterContractsQueried => !m_charContractsMonitor.IsUpdating;

        /// <summary>
        /// Gets or sets a value indicating whether the character industry jobs have been queried.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the character industry jobs have been queried; otherwise, <c>false</c>.
        /// </value>
        internal bool CharacterIndustryJobsQueried => !m_charIndustryJobsMonitor.IsUpdating;

        #endregion


        #region Dispose

        /// <summary>
        /// Called when the object gets disposed.
        /// </summary>
        internal void Dispose()
        {
            EveMonClient.TimerTick -= EveMonClient_TimerTick;

            // Unsubscribe events in monitors
            foreach (IQueryMonitorEx monitor in m_characterQueryMonitors)
            {
                monitor.Dispose();
            }
        }

        #endregion


        #region Querying

        /// <summary>
        /// Queries the character's mailing lists.
        /// </summary>
        private void QueryCharacterMailingLists()
        {
            // Quits if no network
            if (!NetworkMonitor.IsNetworkAvailable)
                return;

            // Quits if access denied
            APIKey apiKey = m_ccpCharacter.Identity.FindAPIKeyWithAccess(CCPAPICharacterMethods.MailingLists);
            if (apiKey == null)
                return;

            EveMonClient.APIProviders.CurrentProvider.QueryMethodAsync<SerializableAPIMailingLists>(
                CCPAPICharacterMethods.MailingLists,
                apiKey.ID,
                apiKey.VerificationCode,
                m_ccpCharacter.CharacterID,
                OnMailingListsUpdated);
        }

        /// <summary>
        /// Queries the character's info.
        /// </summary>
        private void QueryCharacterInfo()
        {
            // Quits if no network
            if (!NetworkMonitor.IsNetworkAvailable)
                return;

            // Quits if access denied
            APIKey apiKey = m_ccpCharacter.Identity.FindAPIKeyWithAccess(CCPAPICharacterMethods.CharacterInfo);
            if (apiKey == null)
                return;

            EveMonClient.APIProviders.CurrentProvider.QueryMethodAsync<SerializableAPICharacterInfo>(
                CCPAPICharacterMethods.CharacterInfo,
                apiKey.ID,
                apiKey.VerificationCode,
                m_ccpCharacter.CharacterID,
                OnCharacterInfoUpdated);
        }

        /// <summary>
        /// Queries the character's contract bids.
        /// </summary>
        private void QueryCharacterContractBids()
        {
            // Quits if no network
            if (!NetworkMonitor.IsNetworkAvailable)
                return;

            // Quits if access denied
            APIKey apiKey = m_ccpCharacter.Identity.FindAPIKeyWithAccess(CCPAPICharacterMethods.Contracts);
            if (apiKey == null)
                return;

            EveMonClient.APIProviders.CurrentProvider.QueryMethodAsync<SerializableAPIContractBids>(
                CCPAPIGenericMethods.ContractBids,
                apiKey.ID,
                apiKey.VerificationCode,
                m_ccpCharacter.CharacterID,
                OnContractBidsUpdated);
        }

        /// <summary>
        /// Processed the queried character's skill queue information.
        /// </summary>
        /// <param name="result"></param>
        private void OnCharacterSheetUpdated(CCPAPIResult<SerializableAPICharacterSheet> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.CharacterSheet))
                EveMonClient.Notifications.NotifyCharacterSheetError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Query the Character's info
            QueryCharacterInfo();

            // Imports the data
            m_ccpCharacter.Import(result);

            // Notify for insufficient balance
            m_ccpCharacter.NotifyInsufficientBalance();

            // Save the file to our cache
            LocalXmlCache.SaveAsync(result.Result.Name, result.XmlDocument).ConfigureAwait(false);
        }

        /// <summary>
        /// Processes the queried character's info.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnCharacterInfoUpdated(CCPAPIResult<SerializableAPICharacterInfo> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occured
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.CharacterInfo))
                EveMonClient.Notifications.NotifyCharacterInfoError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.Import(result.Result);
        }

        /// <summary>
        /// Processes the queried character's skill queue information.
        /// </summary>
        /// <param name="result"></param>
        private void OnSkillQueueUpdated(CCPAPIResult<SerializableAPISkillQueue> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.SkillQueue))
                EveMonClient.Notifications.NotifySkillQueueError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.SkillQueue.Import(result.Result.Queue);

            // Check the account has a character in training (if API key of type "Account")
            APIKey apikey = m_ccpCharacter.Identity.FindAPIKeyWithAccess(CCPAPICharacterMethods.SkillInTraining);
            apikey?.CharacterInTraining();

            // Check the character has less than a day of training in skill queue
            if (m_ccpCharacter.IsTraining && m_ccpCharacter.SkillQueue.LessThanWarningThreshold)
            {
                EveMonClient.Notifications.NotifySkillQueueLessThanADay(m_ccpCharacter);
                return;
            }

            EveMonClient.Notifications.InvalidateSkillQueueLessThanADay(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's standings information.
        /// </summary>
        /// <param name="result"></param>
        private void OnStandingsUpdated(CCPAPIResult<SerializableAPIStandings> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.Standings))
                EveMonClient.Notifications.NotifyCharacterStandingsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.Standings.Import(result.Result.CharacterNPCStandings.All);

            // Fires the event regarding standings update
            EveMonClient.OnCharacterStandingsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's factional warfare statistic information.
        /// </summary>
        /// <param name="result"></param>
        private void OnFactionalWarfareStatsUpdated(CCPAPIResult<SerializableAPIFactionalWarfareStats> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.FactionalWarfareStats))
                EveMonClient.Notifications.NotifyCharacterFactionalWarfareStatsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
            {
                if (result.CCPError == null || !result.CCPError.IsFactionalWarfareEnlistedError)
                    return;

                // Update the enlisted in factional warfare flag
                m_ccpCharacter.IsFactionalWarfareNotEnlisted = true;

                // Fires the event regarding factional warfare stats update
                EveMonClient.OnCharacterFactionalWarfareStatsUpdated(m_ccpCharacter);
                return;
            }

            // Update the enlisted in factional warfare flag
            m_ccpCharacter.IsFactionalWarfareNotEnlisted = false;

            // Import the data
            m_ccpCharacter.FactionalWarfareStats = new FactionalWarfareStats(result.Result);

            // Fires the event regarding factional warfare stats update
            EveMonClient.OnCharacterFactionalWarfareStatsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's assets information.
        /// </summary>
        /// <param name="result"></param>
        private void OnAssetsUpdated(CCPAPIResult<SerializableAPIAssetList> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.AssetList))
                EveMonClient.Notifications.NotifyCharacterAssetsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            TaskHelper.RunCPUBoundTaskAsync(() => m_ccpCharacter.Assets.Import(result.Result.Assets))
                .ContinueWith(_ =>
                {
                    // Fires the event regarding assets update
                    EveMonClient.OnCharacterAssetsUpdated(m_ccpCharacter);

                }, EveMonClient.CurrentSynchronizationContext);
        }

        /// <summary>
        /// Processes the queried character's market orders.
        /// </summary>
        /// <param name="result"></param>
        /// <remarks>This method is sensitive to which "issued for" orders gets queried first</remarks>
        private void OnMarketOrdersUpdated(CCPAPIResult<SerializableAPIMarketOrders> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.MarketOrders))
                EveMonClient.Notifications.NotifyCharacterMarketOrdersError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            result.Result.Orders.ToList().ForEach(order => order.IssuedFor = IssuedFor.Character);

            // Import the data
            List<MarketOrder> endedOrders = new List<MarketOrder>();
            m_ccpCharacter.CharacterMarketOrders.Import(result.Result.Orders, endedOrders);

            // Fires the event regarding character market orders update
            EveMonClient.OnCharacterMarketOrdersUpdated(m_ccpCharacter, endedOrders);
        }

        /// <summary>
        /// Processes the queried character's contracts.
        /// </summary>
        /// <param name="result"></param>
        /// <remarks>This method is sensitive to which "issued for" contracts gets queried first</remarks>
        private void OnContractsUpdated(CCPAPIResult<SerializableAPIContracts> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.Contracts))
                EveMonClient.Notifications.NotifyCharacterContractsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Query the contract bids
            QueryCharacterContractBids();

            result.Result.Contracts.ToList().ForEach(x =>
            {
                x.IssuedFor = IssuedFor.Character;
                x.APIMethod = CCPAPICharacterMethods.Contracts;
            });

            // Import the data
            List<Contract> endedContracts = new List<Contract>();
            m_ccpCharacter.CharacterContracts.Import(result.Result.Contracts, endedContracts);

            // Fires the event regarding character contracts update
            EveMonClient.OnCharacterContractsUpdated(m_ccpCharacter, endedContracts);
        }

        /// <summary>
        /// Processes the queried character's contract bids.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnContractBidsUpdated(CCPAPIResult<SerializableAPIContractBids> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occured
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPIGenericMethods.ContractBids))
                EveMonClient.Notifications.NotifyCharacterContractBidsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.CharacterContractBids.Import(result.Result.ContractBids);

            // Fires the event regarding character contract bids update
            EveMonClient.OnCharacterContractBidsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's wallet journal information.
        /// </summary>
        /// <param name="result"></param>
        private void OnWalletJournalUpdated(CCPAPIResult<SerializableAPIWalletJournal> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.WalletJournal))
                EveMonClient.Notifications.NotifyCharacterWalletJournalError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.WalletJournal.Import(result.Result.WalletJournal);

            // Fires the event regarding wallet journal update
            EveMonClient.OnCharacterWalletJournalUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's wallet transactions information.
        /// </summary>
        /// <param name="result"></param>
        private void OnWalletTransactionsUpdated(CCPAPIResult<SerializableAPIWalletTransactions> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.WalletTransactions))
                EveMonClient.Notifications.NotifyCharacterWalletTransactionsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.WalletTransactions.Import(result.Result.WalletTransactions);

            // Fires the event regarding wallet transactions update
            EveMonClient.OnCharacterWalletTransactionsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's personal industry jobs.
        /// </summary>
        /// <param name="result"></param>
        /// <remarks>This method is sensitive to which "issued for" jobs gets queried first</remarks>
        private void OnIndustryJobsUpdated(CCPAPIResult<SerializableAPIIndustryJobs> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.IndustryJobs))
                EveMonClient.Notifications.NotifyCharacterIndustryJobsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            result.Result.Jobs.ToList().ForEach(x => x.IssuedFor = IssuedFor.Character);

            // Import the data
            m_ccpCharacter.CharacterIndustryJobs.Import(result.Result.Jobs);

            // Fires the event regarding character industry jobs update
            EveMonClient.OnCharacterIndustryJobsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's research points.
        /// </summary>
        /// <param name="result"></param>
        private void OnResearchPointsUpdated(CCPAPIResult<SerializableAPIResearch> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occured
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.ResearchPoints))
                EveMonClient.Notifications.NotifyResearchPointsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.ResearchPoints.Import(result.Result.ResearchPoints);

            // Fires the event regarding research points update
            EveMonClient.OnCharacterResearchPointsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's EVE mail messages.
        /// </summary>
        /// <param name="result"></param>
        private void OnEVEMailMessagesUpdated(CCPAPIResult<SerializableAPIMailMessages> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occured
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.MailMessages))
                EveMonClient.Notifications.NotifyEVEMailMessagesError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Each time we import a new batch of EVE mail messages,
            // query the mailing lists so that we are always up to date
            QueryCharacterMailingLists();

            // Import the data
            m_ccpCharacter.EVEMailMessages.Import(result.Result.Messages);

            // Notify on new messages
            if (m_ccpCharacter.EVEMailMessages.NewMessages != 0)
                EveMonClient.Notifications.NotifyNewEVEMailMessages(m_ccpCharacter, m_ccpCharacter.EVEMailMessages.NewMessages);
        }

        /// <summary>
        /// Processes the queried character's EVE mailing lists.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnMailingListsUpdated(CCPAPIResult<SerializableAPIMailingLists> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occured
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.MailingLists))
                EveMonClient.Notifications.NotifyMailingListsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.EVEMailingLists.Import(result.Result.MailingLists);
        }

        /// <summary>
        /// Processes the queried character's EVE notifications.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnEVENotificationsUpdated(CCPAPIResult<SerializableAPINotifications> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occured
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.Notifications))
                EveMonClient.Notifications.NotifyEVENotificationsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.EVENotifications.Import(result.Result.Notifications);

            // Notify on new notifications
            if (m_ccpCharacter.EVENotifications.NewNotifications != 0)
                EveMonClient.Notifications.NotifyNewEVENotifications(m_ccpCharacter,
                                                                     m_ccpCharacter.EVENotifications.NewNotifications);
        }

        /// <summary>
        /// Processes the queried character's contact list.
        /// </summary>
        /// <param name="result"></param>
        private void OnContactsUpdated(CCPAPIResult<SerializableAPIContactList> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.ContactList))
                EveMonClient.Notifications.NotifyCharacterContactsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.Contacts.Import(result.Result.AllContacts);

            // Fires the event regarding contacts update
            EveMonClient.OnCharacterContactsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's medals.
        /// </summary>
        /// <param name="result"></param>
        private void OnMedalsUpdated(CCPAPIResult<SerializableAPIMedals> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.Medals))
                EveMonClient.Notifications.NotifyCharacterMedalsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.CharacterMedals.Import(result.Result.CharacterAllMedals);

            // Fires the event regarding medals update
            EveMonClient.OnCharacterMedalsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's kill log.
        /// </summary>
        /// <param name="result"></param>
        private void OnKillLogUpdated(CCPAPIResult<SerializableAPIKillLog> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.KillLog))
                EveMonClient.Notifications.NotifyCharacterKillLogError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.KillLog.Import(result.Result.Kills);

            // Fires the event regarding kill log update
            EveMonClient.OnCharacterKillLogUpdated(m_ccpCharacter);

            // Save the file to our cache
            string filename = $"{m_ccpCharacter.Name}-{CCPAPICharacterMethods.KillLog}";
            LocalXmlCache.SaveAsync(filename, result.XmlDocument).ConfigureAwait(false);
        }

        /// <summary>
        /// Processes the queried character's upcoming calendar events.
        /// </summary>
        /// <param name="result"></param>
        private void OnUpcomingCalendarEventsUpdated(CCPAPIResult<SerializableAPIUpcomingCalendarEvents> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPICharacterMethods.UpcomingCalendarEvents))
                EveMonClient.Notifications.NotifyCharacterUpcomindCalendarEventsError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Import the data
            m_ccpCharacter.UpcomingCalendarEvents.Import(result.Result.UpcomingEvents);

            // Fires the event regarding upcoming calendar events update
            EveMonClient.OnCharacterUpcomingCalendarEventsUpdated(m_ccpCharacter);
        }

        /// <summary>
        /// Processes the queried character's palnetary colonies.
        /// </summary>
        /// <param name="result"></param>
        private void OnPlanetaryColoniesUpdated(CCPAPIResult<SerializableAPIPlanetaryColonies> result)
        {
            // Character may have been deleted or set to not be monitored since we queried
            if (m_ccpCharacter == null || !m_ccpCharacter.Monitored)
                return;

            // Notify an error occurred
            if (m_ccpCharacter.ShouldNotifyError(result, CCPAPIGenericMethods.PlanetaryColonies))
                EveMonClient.Notifications.NotifyCharacterPlanetaryColoniesError(m_ccpCharacter, result);

            // Quits if there is an error
            if (result.HasError)
                return;

            // Invalidate previous notifications
            EveMonClient.Notifications.InvalidateCharacterPlanetaryPinCompleted(m_ccpCharacter);

            // Import the data
            m_ccpCharacter.PlanetaryColonies.Import(result.Result.Colonies);

            // Fires the event regarding planetary colonies update
            EveMonClient.OnCharacterPlanetaryColoniesUpdated(m_ccpCharacter);
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// Handles the TimerTick event of the EveMonClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_TimerTick(object sender, EventArgs e)
        {
            // If character is monitored enable the basic feature monitoring
            m_basicFeaturesMonitors.ForEach(monitor => monitor.Enabled = m_ccpCharacter.Monitored);
        }

        #endregion
    }
}