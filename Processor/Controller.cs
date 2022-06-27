namespace Processor
{
    public class Controller
    {
        private bool[] _buttonStates;
        private bool _strobe;
        private int _currentButton;
        public enum Button
        {
            A = 0,
            B,
            Select,
            Start,
            Up,
            Down,
            Left,
            Right
        };
        public Controller()
        {
            _buttonStates = new bool[8];
            _strobe = false;
            _currentButton = 0;
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
            if (_currentButton > 7) 
                return 1;

            byte state = (byte)(_buttonStates[_currentButton] ? 1 : 0);

            if (!_strobe)
                _currentButton++;

            return state;
        }
    }

}