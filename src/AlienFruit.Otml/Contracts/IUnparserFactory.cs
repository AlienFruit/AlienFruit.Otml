using System;
using System.Text;

namespace AlienFruit.Otml
{
    public interface IUnparserFactory
    {
        IUnparser GetUnparser(Version version, Encoding encoding);

        IUnparser GetDefaultUnparser(Encoding encoding);
    }
}