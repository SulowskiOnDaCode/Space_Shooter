using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wraith : Class
{
    public Wraith()
    {
        name = "Wraith";
        hp = 2;
        movementSpeed = 11f;
        shootingCooldown = 0.2f;
        ability = new Teleport();
    }
}
