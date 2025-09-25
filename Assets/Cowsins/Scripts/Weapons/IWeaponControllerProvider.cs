using UnityEngine;

namespace cowsins
{
    // Implemented by WeaponController and required by PlayerDependencies
    public interface IWeaponReferenceProvider
    {
        public Camera MainCamera { get; }
        Weapon_SO Weapon { get; set; }
        WeaponIdentification Id { get; set; }
        public int CurrentWeaponIndex {  get; set; }
        public WeaponIdentification[] Inventory { get; set; }
        public int InventorySize { get; }
    }
    public interface IWeaponBehaviourProvider
    {
        bool IsAiming { get; }
        bool Reloading { get; set; }
        bool CanShoot { get; }
        bool IsShooting { get; set; }
        bool RemoveCrosshairOnAiming { get; }
        bool AlternateAiming { get; }
        bool IsMeleeAvailable { get; set;  }
        void ReleaseCurrentWeapon(); 
    }
    public interface IWeaponRecoilProvider
    {
        float RecoilPitchOffset { get; }
        float RecoilYawOffset { get; }
    }
}