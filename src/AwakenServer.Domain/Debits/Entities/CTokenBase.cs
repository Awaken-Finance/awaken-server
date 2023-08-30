using System;
using AwakenServer.Entities;
using JetBrains.Annotations;
using Nest;

namespace AwakenServer.Debits.Entities
{
    public class CTokenBase : MultiChainEntity<Guid>
    {
        [Keyword] public override Guid Id { get; set; }
        [Keyword] public Guid CompControllerId { get; set; }
        [Keyword] [NotNull] public virtual string Address { get; set; }

        [Keyword] [NotNull] public virtual string Symbol { get; set; }

        public virtual int Decimals { get; set; }
    }

    public class EditableCTokenBase : CTokenBase
    {
        public string TotalCTokenMintAmount { get; set; }
        public string TotalUnderlyingAssetBorrowAmount { get; set; }
        public string TotalUnderlyingAssetReserveAmount { get; set; }
        public string TotalUnderlyingAssetAmount { get; set; }
        public bool IsBorrowPaused { get; set; }
        public bool IsMintPaused { get; set; }
        public bool IsList { get; set; }
        public string BorrowCompSpeed { get; set; }
        public string AccumulativeBorrowComp { get; set; }
        public string SupplyCompSpeed { get; set; }
        public string AccumulativeSupplyComp { get; set; }
        public string CollateralFactorMantissa { get; set; }
        public string ReserveFactorMantissa { get; set; }
    }
}