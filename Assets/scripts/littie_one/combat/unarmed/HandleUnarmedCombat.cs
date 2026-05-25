using UnityEngine;
using System.Collections; // Add this namespace

public partial class TribeMember
{
    private bool isanimationPosition = false;
    private float speadofanimationposition = 0;
    private Vector3 animationPosition;
    private void HandleUnarmedCombat()
    {

        if(level >=1)
        hookpunsh();
    }
}