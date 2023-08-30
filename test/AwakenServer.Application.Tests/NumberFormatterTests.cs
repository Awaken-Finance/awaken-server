using System.Collections.Generic;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer;

public class NumberFormatterTests: AwakenServerApplicationTestBase
{
    [Fact]
    public void FormatByDecimalsTest()
    {
        var number = 1L.ToDecimalsString(18);
        number.ShouldBe("0.000000000000000001");
        
        number = long.MaxValue.ToDecimalsString( 18);
        number.ShouldBe("9.223372036854776");
        
    }

    [Fact]
    public void FormatTest()
    {
        var value = "0.0559014";
        var num = BigDecimal.Parse(value);
        num.ToNormalizeString().ShouldBe(value);
        
        value = "10000.0559014";
        num = BigDecimal.Parse(value);
        num.ToNormalizeString().ShouldBe(value);
        
        value = "1000000000000000000000000000";
        num = BigDecimal.Parse(value);
        num.ToNormalizeString().ShouldBe(value);
        
        value = "-0.0559014";
        num = BigDecimal.Parse(value);
        num.ToNormalizeString().ShouldBe(value);
        
        value = "-0.00000000014";
        num = BigDecimal.Parse(value);
        num.ToNormalizeString().ShouldBe(value);
        
        value = "-100000.0559014";
        num = BigDecimal.Parse(value);
        num.ToNormalizeString().ShouldBe(value);
        
        value = "-1000000000000000000000000000.00000000001";
        num = BigDecimal.Parse(value);
        num.ToNormalizeString().ShouldBe(value);

        //5591816
        //222102598
        //710737893
        //96410777
        var amount = "0.0559014";
        var lps = new List<long>();
        lps.Add(5591816);
        lps.Add(222102598);
        lps.Add(710737893);
        lps.Add(96410777);

        foreach (var lp in lps)
        {
            var a = BigDecimal.Parse(amount);
            var l = lp.ToDecimalsString(8);
            a -= BigDecimal.Parse(l);
            amount = a.ToNormalizeString();
        }
    }
}