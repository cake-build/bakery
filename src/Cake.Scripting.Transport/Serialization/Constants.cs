namespace Cake.Scripting.Transport.Serialization
{
    internal static class Constants
    {
        public static class CakeScript
        {
            public static readonly byte TypeId = 0x00;
            public static readonly byte Version = 0x00;
            public static readonly short TypeAndVersion = (short)(TypeId << 8 | Version);
        }

        public static class FileChange
        {
            public static readonly byte TypeId = 0x01;
            public static readonly byte Version = 0x00;
            public static readonly short TypeAndVersion = (short)(TypeId << 8 | Version);
        }
    }
}
