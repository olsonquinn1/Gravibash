using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ProjectileManager : ScriptableObject
{
    public string name;
    public GameObject projectilePrefab;

    //prefab properties 
    //time between shots
    public int projectileBaseFireRate;
    //Vel of projectile just shot
    public int projectileBaseVel;
    //Dmg calculated using Dmg=Vel^2*BaseDmg/(BaseVel^2)
    //--Vel calculated using the sum of the player and character vel vectors
    public int projectileBaseDamage;
    //projectile standard dev in deg
    public float projectileBaseAcc;
    public int projectileBurstSize;
    //burst fire rate as percent of minimum fire time
    public float projectileBurstRate;
    //base capacity of magazine
    public int reloadSize;
    //time to swap magazine
    public float reloadTime;
    //side note: no repacking mags one ammo pool

    //factor of (m/s)/s lost based on current speed aka looping differential eqn
    public int projectileDragCoef;
    //controlls the acceleration added by gravity vector
    public float projectileGravFactor;
    //projectiles own forward propultion
    public float projectileThrustAccel;
    //projectile mouse tracking strength
    public float projectileTurnStrength;
    //projectile spin damping
    public float projectileTurnDamping;
    //how bouncy is it
    public float projectileElacticity;

    //how much character gets pushed
    public float recoilForce;
    //how much the mouse aim gets changed (in deg)
    public float recoilAimChange;
}
