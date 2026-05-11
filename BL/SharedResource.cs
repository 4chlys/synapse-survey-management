namespace SynapseEnqueteManagement.BL;

// Marker class for IStringLocalizer<SharedResource> — resource files at Resources/SharedResource.[culture].resx.
// Must live in the project root (not inside Resources/) so MSBuild does not treat it as a code-behind for the
// adjacent .resx files. When co-located, MSBuild derives the resource logical name from the class namespace,
// dropping the Resources/ path component, which breaks AddLocalization(ResourcesPath = "Resources").
public class SharedResource { }
