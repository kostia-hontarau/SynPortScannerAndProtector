using System;

using PcapDotNet.Packets.IpV4;

using NetFwTypeLib;


namespace lab2.ScanProtecting.ScanReactors
{
    internal sealed class BlockIPScanningReaction : IScanningReaction
    {
        #region Constants
        const string guidFWPolicy2 = "{E2B3C97F-6AE1-41AC-817A-F6F92166D7DD}";
        const string guidRWRule = "{2C5BC43E-3369-4C33-AB0C-BE9469677AF4}"; 
        #endregion

        #region Members
        public void React(IpV4Address scanner)
        {
            try
            {
                INetFwPolicy2 fwPolicy = GetNetFwPolicy();
                if (ExistsBlockingRuleFor(scanner, fwPolicy)) return;

                Console.WriteLine("Adding firewall rule...");
                INetFwRule newRule = SetupNetFwRule(scanner);
                newRule.Profiles = fwPolicy.CurrentProfileTypes;
                fwPolicy.Rules.Add(newRule);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new InvalidOperationException("You don't have rights for changing firewall rules without administrator rights.", exception);
            }
        }
        #endregion

        #region Assistants
        private bool ExistsBlockingRuleFor(IpV4Address scanner, INetFwPolicy2 fwPolicy)
        {
            INetFwRules rules = fwPolicy.Rules;
            foreach (INetFwRule rule in rules)
            {
                bool isAppMadeRule = rule.Name == "AntiScan_Rule";
                string addresses = rule.RemoteAddresses;
                int slashPos = addresses.IndexOf('/');
                string address = slashPos > 0 ? addresses.Substring(0, slashPos) : String.Empty;
                bool isScannerBlocked = address == scanner.ToString();
                if (isAppMadeRule && isScannerBlocked) return true;
            }
            return false;
        }

        private INetFwRule SetupNetFwRule(IpV4Address scanner)
        {
            Guid clsid = new Guid(BlockIPScanningReaction.guidRWRule);
            Type typeFWRule = Type.GetTypeFromCLSID(clsid);
            INetFwRule newRule = (INetFwRule)Activator.CreateInstance(typeFWRule);
            newRule.Name = "AntiScan_Rule";
            newRule.Description = String.Format("Block inbound traffic from {0}", scanner.ToString());
            newRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
            newRule.RemoteAddresses = scanner.ToString();
            newRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            newRule.Enabled = true;
            newRule.Grouping = "AntiScanning rule added from clr...";
            newRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            return newRule;
        }
        private INetFwPolicy2 GetNetFwPolicy()
        {
            Guid clsid = new Guid(BlockIPScanningReaction.guidFWPolicy2);
            Type typeFWPolicy2 = Type.GetTypeFromCLSID(clsid);
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(typeFWPolicy2);
            return fwPolicy2;
        } 
        #endregion
    }
}
