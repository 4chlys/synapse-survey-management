using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Domain;

// Marker for translation entities so TranslationPicker can apply the universal
// "translation in this language, else first translation" fallback in one place.
public interface IHasLanguage
{
    Language Language { get; set; }
}
