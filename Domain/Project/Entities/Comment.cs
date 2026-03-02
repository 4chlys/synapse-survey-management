namespace SynapseEnqueteManagement.BL.Domain.Project.Entities;

public abstract class Comment(Idea idea)
{
    protected Idea Idea { get; set; } = idea;
}