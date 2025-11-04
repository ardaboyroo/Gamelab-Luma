using System;
using Unity.Netcode;

namespace UserModels.Display
{
    [Serializable]
    public struct AvatarData : INetworkSerializable, IEquatable<AvatarData>
    {
        public bool Initialized;

        public int HairID;
        public int HairColorID;
        public int EyesID;
        public int EyeColorID;
        public int BrowsID;
        public int MouthID;
        public int NoseID;
        public int OutfitID;


        // --- Network Serialization ---
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Initialized);
            serializer.SerializeValue(ref HairID);
            serializer.SerializeValue(ref HairColorID);
            serializer.SerializeValue(ref EyesID);
            serializer.SerializeValue(ref EyeColorID);
            serializer.SerializeValue(ref BrowsID);
            serializer.SerializeValue(ref MouthID);
            serializer.SerializeValue(ref NoseID);
            serializer.SerializeValue(ref OutfitID);
        }

        // --- Equality ---
        public bool Equals(AvatarData o)
        {
            return Initialized == o.Initialized &&
                   HairID == o.HairID &&
                   HairColorID == o.HairColorID &&
                   EyesID == o.EyesID &&
                   EyeColorID == o.EyeColorID &&
                   BrowsID == o.BrowsID &&
                   MouthID == o.MouthID &&
                   NoseID == o.NoseID &&
                   OutfitID == o.OutfitID;
        }

        public override bool Equals(object obj) => obj is AvatarData other && Equals(other);

        public override int GetHashCode()
        {
            // Compact, fast, low collision rate for structs of ints
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Initialized.GetHashCode();
                hash = hash * 31 + HairID;
                hash = hash * 31 + HairColorID;
                hash = hash * 31 + EyesID;
                hash = hash * 31 + EyeColorID;
                hash = hash * 31 + BrowsID;
                hash = hash * 31 + MouthID;
                hash = hash * 31 + NoseID;
                hash = hash * 31 + OutfitID;
                return hash;
            }
        }

        public static bool operator ==(AvatarData a, AvatarData b) => a.Equals(b);
        public static bool operator !=(AvatarData a, AvatarData b) => !a.Equals(b);

        // --- Utility Helpers ---
        public static AvatarData Default => new AvatarData
        {
            Initialized = false,
            HairID = 0,
            HairColorID = 0,
            EyesID = 0,
            EyeColorID = 0,
            BrowsID = 0,
            MouthID = 0,
            NoseID = 0,
            OutfitID = 0
        };

        public override string ToString()
        {
            return $"AvatarData(Hair:{HairID}, HairColor:{HairColorID}, Eyes:{EyesID}, EyeColor:{EyeColorID}, " +
                   $"Brows:{BrowsID}, Mouth:{MouthID}, Nose:{NoseID}, Outfit:{OutfitID}, Init:{Initialized})";
        }
    }

    [Serializable]
    public struct PlayerProfile : INetworkSerializable, IEquatable<PlayerProfile>
    {
        public string Nickname;
        public AvatarData Avatar;

        public PlayerProfile(string nickname, AvatarData avatar)
        {
            Nickname = nickname;
            Avatar = avatar;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Nickname);
            serializer.SerializeValue(ref Avatar);
        }

        public bool Equals(PlayerProfile other) => Nickname == other.Nickname && Avatar.Equals(other.Avatar);
    }
}
