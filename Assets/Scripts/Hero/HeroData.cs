using UnityEngine;
using System;

namespace Photon.Pun.Demo.Asteroids
{
    [Serializable]
    public class HeroData
    {
        public int Id;
        public string Name;
        public string Description;
        public Sprite AvatarSprite;
        public string PrefabName; // Name of the prefab to instantiate for this hero
    }
}