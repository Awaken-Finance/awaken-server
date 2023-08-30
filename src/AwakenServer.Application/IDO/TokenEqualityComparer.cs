using System.Collections.Generic;
using AwakenServer.Tokens;

namespace AwakenServer.IDO
{
    public class TokenEqualityComparer: IEqualityComparer<Token>
    {
        public bool Equals(Token x, Token y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Id == y.Id;
        }

        public int GetHashCode(Token obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}