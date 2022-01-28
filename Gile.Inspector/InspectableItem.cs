﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.LayerManager;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gile.AutoCAD.Inspector
{
    /// <summary>
    /// Type bounded to the items of the TreeView control.
    /// </summary>
    public class InspectableItem : ItemBase
    {
        public IEnumerable<InspectableItem> Children { get; protected set; }
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        public string Name { get; protected set; }

        public InspectableItem(object value, bool isSelected = false, bool isExpanded = false, IEnumerable<InspectableItem> children = null, string name = null)
            : base(value)
        {
            Children = children;
            IsSelected = isSelected;
            IsExpanded = isExpanded;
            if (value is ObjectId id)
                Initialize(id, name);
            else if (value is LayerFilter filter)
                Initialize(filter);
            else
                Name = name ?? Label;
        }

        private void Initialize(LayerFilter filter)
        {
            Name = filter.Name;
            Children = filter.NestedFilters.Cast<LayerFilter>().Select(f => new InspectableItem(f));
        }

        private void Initialize(ObjectId id, string name)
        {
            using (var tr = id.Database.TransactionManager.StartTransaction())
            {
                var dbObj = tr.GetObject(id, OpenMode.ForRead);
                if (string.IsNullOrEmpty(name))
                {
                    Name = dbObj is SymbolTableRecord r ? r.Name : $"< {dbObj.GetType().Name}\t{dbObj.Handle} >";
                }
                else 
                {
                    if (name == "<_>")
                        Name = $"< {dbObj.GetType().Name}\t{dbObj.Handle} >";
                    else
                        Name = name;
                }

                if (dbObj is SymbolTable)
                {
                    Children = ((SymbolTable)dbObj)
                        .Cast<ObjectId>()
                        .Select(x => new InspectableItem(x));
                }
                else if (dbObj is DBDictionary dict)
                {
                    if (id == id.Database.NamedObjectsDictionaryId)
                    {
                        Name = "Named Objects Dictionary";
                        IsExpanded = true;
                    }
                    Children = ((DBDictionary)dbObj)
                        .Cast<DictionaryEntry>()
                        .Select(e => new InspectableItem((ObjectId)e.Value, name: (string)e.Key));
                }
            }
        }
    }
}
