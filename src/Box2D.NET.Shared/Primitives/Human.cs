namespace Box2D.NET.Shared.Primitives
{
    public class Human
    {
        public readonly Bone[] bones = new Bone[(int)BoneId.boneId_count];
        public float scale;
        public bool isSpawned;

        public void Clear()
        {
            for (int i = 0; i < bones.Length; ++i)
            {
                bones[i] = new Bone();
            }
            scale = 0.0f;
            isSpawned = false;
        }
    }
}