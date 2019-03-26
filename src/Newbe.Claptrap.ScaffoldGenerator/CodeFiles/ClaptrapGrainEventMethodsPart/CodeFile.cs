using System.Collections.Generic;

namespace Newbe.Claptrap.ScaffoldGenerator.CodeFiles.ClaptrapGrainEventMethodsPart
{
    public class CodeFile : ICodeFile
    {
        public string StateDataTypeFullName { get; set; }
        public string ClaptrapCatalog { get; set; }
        public string ClassName { get; set; }
        public string InterfaceName { get; set; }
        public IEnumerable<EventMethod> EventMethods { get; set; }
    }
}