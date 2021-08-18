namespace SignatureProcessor.Processor
{
    /// <summary>
    /// The coordinates are relative to screen coordinate space. For example, if you draw
    /// the<input> element with style = "width:100%;height:50%", on a P400, the coordinates
    /// will be in the space of 0:0 to 240:320. Whereas on an M400, it’ll be 0:0 to 240:854
    /// 
    /// A special coordinate - {"t":0,"x":-1,"y":-1} is used to separate different signature strokes.
    /// 
    /// </summary>
    public class SignatureObject
    {
        public int t;   // timestamp when point was recorder
        public int x;   // x coordinate of the signature point
        public int y;   // y coordinate of the signature point
    }
}
