public struct AudioHandle
{
    public readonly int id;
    public AudioHandle(int id) => this.id = id;
    public static readonly AudioHandle Invalid = new AudioHandle(-1);
    public bool IsValid => id >= 0;
}