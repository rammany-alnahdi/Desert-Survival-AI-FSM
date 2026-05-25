using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 10f;
    public float lifetime = 5f;
    
    private Transform target;
    private Vector3 targetLastPosition;
    
    public void SetDamage(float dmg) => damage = dmg;
    public void SetTarget(Transform tgt)
    {
        target = tgt;
        if (target != null) targetLastPosition = target.position;
    }
    
    void Update()
    {
        if (target != null)
            targetLastPosition = target.position;
            
        Vector3 moveDir = (targetLastPosition - transform.position).normalized;
        transform.position += moveDir * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(moveDir);
        
        // Check hit
        if (Vector3.Distance(transform.position, targetLastPosition) < 0.5f)
        {
            Explode();
        }
        
        lifetime -= Time.deltaTime;
        if (lifetime <= 0) Destroy(gameObject);
    }
    
    void Explode()
    {
        // Check for hits in radius
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hit in hits)
        {
            TribeMember member = hit.GetComponent<TribeMember>();
            if (member != null)
            {
                member.TakeDamage(damage);
            }
        }
        Destroy(gameObject);
    }
}