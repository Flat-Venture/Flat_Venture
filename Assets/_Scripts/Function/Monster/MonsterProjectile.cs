using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MonsterProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float destroyDistance = 20f;
    Vector3 moveDir;
    Vector3 startPosition;
    public MonsterBase owner;
    int playerLayer;

    public ParticleSystem fx;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void OnEnable()
    {
        fx.Clear();
        fx.Play();
    }

    void Update()
    {
        fire();
    }

    public void Init(MonsterBase owner, Vector3 dir)
    {
        this.owner = owner;
        startPosition = transform.position;
        moveDir = dir.normalized;

        //if (fx != null)
        //{
        //    fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        //    fx.Clear(true);
        //    fx.Play(true);
        //}
        PlaySfx("MonsterProjectile");
        gameObject.SetActive(true);
        
    }

    public void fire()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        float sqrDistaance = (startPosition - transform.position).sqrMagnitude;
        if (sqrDistaance > destroyDistance * destroyDistance)
        {
            ReloadPool();
            return;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == playerLayer)
        {
            Player player = other.GetComponent<Player>();
            if (player != null && owner != null && owner.md != null)
            {
                float dmg = owner.md.attackDamage;

                BuffReceiver buff = owner.GetComponent<BuffReceiver>();
                if (buff != null)
                {
                    dmg *= buff.AttackMultiplier;
                }

                player.TakeDamage(dmg);
                ReloadPool(); //맞으면 탄환 회수

            }
        }
    }
    //public void ReloadPool()
    //{
    //    if (fx != null)
    //        fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);

    //    gameObject.SetActive(false);
    //    PoolManager.Instance.Return(Enums.PoolType.MonsterProjectile, this);
    //}


    public void ReloadPool()
    {
        if (owner != null)
        {
            //owner.ReturnMonsterProjectile(this);
            //fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            PoolManager.Instance.Return(Enums.PoolType.MonsterProjectile, this);
        }
        else
        {
            //몬스터가 죽어도 계속 날아가려면 이거 지워야함 아마도 
            //Destroy(gameObject);
        }
    }

    void PlaySfx(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
