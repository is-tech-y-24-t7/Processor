namespace Processor;

public class Controller
{
    private bool[] _buttonStates;
    private bool _strobe;
    private int _currentButton;
    public Controller()
    {
        _buttonStates = new bool[8];
        _strobe = false;
        _currentButton = 0;
    }
    public void setButtonState(Button button, bool state)
    {
        _buttonStates[(int)button] = state;
    }
    
    public void WriteControllerInput(byte input)
    {
        _strobe = (input & 1) != 0;
        if (_strobe) 
            _currentButton = 0;
    }
    
    public void SetButtonState(Button button, bool state)
    {
        _buttonStates[(int)button] = state;
    }
    
    public byte ReadControllerOutput()
    {
        if (!_strobe)
            _currentButton++;

        return (byte)(_buttonStates[_currentButton] ? 1 : 0);
    }
}