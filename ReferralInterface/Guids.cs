// Guids.cs
// MUST match guids.h
using System;

namespace wnxd.ReferralInterface
{
    static class GuidList
    {
        public const string guidReferralInterfacePkgString = "cd99437d-2833-4f58-9de8-c753f1843b07";
        public const string guidReferralInterfaceCmdSetString = "35e69ffb-6e3f-430d-a028-d605d8ff4dc8";

        public static readonly Guid guidReferralInterfaceCmdSet = new Guid(guidReferralInterfaceCmdSetString);
    };
}