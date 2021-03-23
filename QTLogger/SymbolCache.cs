using StonkAtlas.QTLogger.QuestradeAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace StonkAtlas.QTLogger
{
    class SymbolComparer : IEqualityComparer<Symbol>
    {
        public bool Equals(Symbol sym1, Symbol sym2)
        {
            if (sym1 == null && sym2 == null) { return true; }
            if (sym1 == null | sym2 == null) { return false; }
            if (sym1.symbolId == sym2.symbolId) { return true; }
            return false;
        }
        public int GetHashCode(Symbol t)
        {
            return t.symbolId.GetHashCode();
        }
    }

    /// <summary>
    /// In memory symbols cache
    /// Contains all known symbol information
    /// </summary>
    class SymbolCache
    {
        public HashSet<Symbol> _knownSymbols = new HashSet<Symbol>(new SymbolComparer());
        public ImmutableHashSet<Symbol> _newDiscoveredSymbols;

        public SymbolCache()
        {

        }

        /// <summary>
        /// Add newly discovered to in memory list 
        /// </summary>
        /// <param name="symbols"></param>
        public void Add(Symbol[] symbols)
        {
            var newSymbols = ImmutableHashSet.CreateRange(new SymbolComparer(), symbols);
            //Union with other new symbols
            if (_newDiscoveredSymbols != null)
                _newDiscoveredSymbols = _newDiscoveredSymbols.Union(newSymbols);
            else
                _newDiscoveredSymbols = newSymbols;
        }


    }
}
