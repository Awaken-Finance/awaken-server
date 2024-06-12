using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using Volo.Abp.Threading;

namespace AwakenServer.Price
{
    public class PriceAppServiceTestBase : AwakenServerTestBase<PriceAppServiceTestModule>
    {
        
        protected PriceAppServiceTestBase()
        {
            
        }
        
    }
}