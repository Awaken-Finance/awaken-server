using System;

namespace AwakenServer.Debits.DebitAppDto
{
    public class CTokenDto : DebitTokenDto
    {
        public string ChainId { get; set; }
        public Guid CompControllerId { get; set; }
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
        public DebitTokenDto UnderlyingToken { get; set; }
        public double SupplyApy { get; set; }
        public double BorrowApy { get; set; }
        public double SupplyInterest { get; set; }
        public double BorrowInterest { get; set; }
    }
}