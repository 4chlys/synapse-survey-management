using SynapseEnqueteManagement.BL;

namespace ConsoleApp1;

public class ConsoleUi
{
    private IManager _manager;
    public ConsoleUi(IManager manager)
    {
        _manager = manager;
    }

    public void Run()
    {
        //Greet, ask question, save answer
    }
}