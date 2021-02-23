using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBoid : MonoBehaviour
{
    public BoidController BoidControllerRef;

    BoidController.Boid _playerBoid;

    void Start()
    {
        _playerBoid = new BoidController.Boid();

        _playerBoid.position = new Vector4(0.0f, 1.8f, 0.0f);
        _playerBoid.velocity = new Vector4(Globals.PlanetGravity * 0.2f * Random.Range(-1.0f, 1.0f), 0.0f, Globals.PlanetGravity * 0.2f * Random.Range(-1.0f, 1.0f));
        _playerBoid.radius = 0.066f;

        BoidControllerRef.RegisterPlayer(_playerBoid);
    }

    void Update()
    {
        BoidControllerRef.UpdatePlayer(_playerBoid);
    }
}
