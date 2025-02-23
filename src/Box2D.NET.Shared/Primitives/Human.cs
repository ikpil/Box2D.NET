namespace Box2D.NET.Shared.Primitives
{
    public class Human
    {
        public readonly Bone[] bones = new Bone[(int)BoneId.boneId_count];
        public float scale;
        public bool isSpawned;
    }

}