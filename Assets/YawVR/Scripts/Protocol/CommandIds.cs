namespace YawVR
{
    /// <summary>
    /// TCP and UDP command byte identifiers for the current YAW 3 wire protocol.
    /// See the comment at the top of <see cref="CommandEncoder"/> for the overall
    /// message format.
    /// </summary>
    public static class CommandIds
    {
        public const byte CHECK_IN = 0x30;
        public const byte START = 0xA1;
        public const byte STOP = 0xA2;
        public const byte CALIBRATE = 0x55;
        public const byte GET_ALL_APP_PARAMS = 0xF6;
        public const byte EXIT = 0xA3;

        // Defined by the firmware but not currently sent/handled by this SDK.
        // See firmware-todo.md if you want the SDK to start using these.
        public const byte RESET_PORTS = 0x01;
        public const byte SET_SIMU_INPUT_PORT = 0x10;
        public const byte SET_GAME_INPUT_PORT = 0x11;
        public const byte SET_GAME_IP_ADDRESS = 0xA4;
        public const byte SET_OUTPUT_PORT = 0x12;
        public const byte ERROR = 0xA5;

        public const byte SET_POWER = 0x32;
        public const byte CHECK_IN_ANS = 0x31;

        public const byte GET_TEMPS = 0xE4;
        public const byte GET_STATE = 0xE5;

        // UDP commands
        public const byte UDP_LED_CMD = 0xB2;
    }
}
