using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    bool released = true;
    int iter = 0;
    int iter_released = -int.MaxValue;
    GameObject anchoredObject = null;
    float x_anchored, y_anchored, z_anchored = 0;
    Rigidbody rigidbody;

    void OnTriggerEnter(Collider other)
    {
        //if (anchoredObject != null) return;
        var objectcolliding = findParentWithTag("cube", other.gameObject);
        print("other gameobject is: " + other.gameObject);
        print("object colliding is: " + objectcolliding);
        if (objectcolliding == null) return;
        //if (released)
        //{
        foreach (var refCube in GameObject.FindGameObjectsWithTag("refCube"))
            if ((refCube != this.gameObject) && (refCube.GetComponent<CollisionDetector>().IsNotReleased(objectcolliding.name))) return;

        iter = 0;
        print("Collider game object is " + other.gameObject.name);

        anchoredObject = objectcolliding;
        if (anchoredObject == null) return;
        rigidbody = anchoredObject.GetComponent<Rigidbody>();
        //if (rigidbody.velocity==Vector3.zero)             
        //rigidbody.isKinematic = true;
        x_anchored = this.gameObject.transform.position.x;
        y_anchored = anchoredObject.transform.position.y;
        z_anchored = this.gameObject.transform.position.z;
        anchoredObject.transform.position = new Vector3(x_anchored, y_anchored, z_anchored);

        print("isKinematic changed to true");

        released = false;
        print("Another object has ENTERED the trigger and y= " + anchoredObject.transform.position.y);

        //}


    }
    private void OnTriggerExit(Collider other)
    {
        if (anchoredObject == null) return;
        if (anchoredObject != findParentWithTag("cube", other.gameObject)) return;
        if (!released)
        {
            // this is to avoid the ill effects of OntriggerExit/enter called multiple times when isKinematic is false
            if (iter < 15) return;
            //var rigidbody = anchoredObject.GetComponent<Rigidbody>();           
            //rigidbody.isKinematic = false;
            //rigidbody.WakeUp();
            print("Another object has EXITED the trigger, other object is:" + other.gameObject.name);
            iter_released = iter;
            print("isKinematic is = " + rigidbody.isKinematic + " .. " + rigidbody.IsSleeping());
            print("anchored object is: " + anchoredObject.name);
            anchoredObject = null;
            //released = true;


        }

    }
    // Start is called before the first frame update
    void Start()
    {

    }

    GameObject findParentWithTag(string tagToFind)
    {
        return findParentWithTag(tagToFind, this.gameObject);
    }
    GameObject findParentWithTag(string tagToFind, GameObject startingObject)
    {

        if (startingObject.tag.Equals(tagToFind))
            return startingObject;
        var parent = startingObject.transform.parent;
        while (parent != null)
        {
            if (parent.tag == tagToFind)
            {
                return parent.gameObject as GameObject;
            }
            parent = parent.transform.parent;
        }
        return null;
    }
    // Update is called once per frame
    void Update()
    {
        iter++;

        if (rigidbody != null)
        {

            // needed because when releasing object by opening hand it is set by oculus SDK to kinematic
            // and is not affected by Physics
            if ((!released) && (rigidbody.isKinematic) && (iter == iter_released + 10))
            {
                //rigidbody.isKinematic = false;
                iter_released = -int.MaxValue;
                released = true;
                print("Released!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
            if ((released) && (rigidbody.isKinematic))
            {
                rigidbody.isKinematic = false;
                anchoredObject = null;
                rigidbody = null;


                //rigidbody.velocity = Vector3.zero;
            }

        }
        if (anchoredObject != null)
        {

            if ((!released) && (iter == 10))
            {

                rigidbody.isKinematic = true;
                foreach (var gameobject in GameObject.FindGameObjectsWithTag("refCube"))
                    gameobject.GetComponent<CollisionDetector>().releaseRigidBody(this.gameObject.name, rigidbody.gameObject.name);

            }
            //print("anchored object is " + anchoredObject.ToString());
            var eulerAngles = anchoredObject.transform.rotation.eulerAngles;
            // This code is to automatically adjust to 0 the rotation of the cube around the axis that is up
            // helps the user to have the cubes always well aligned on the table
            //anchoredObject.transform.position = new Vector3(x_anchored, y_anchored, z_anchored);

            if (rigidbody.isKinematic)
            {

                /*if (Mathf.Abs(Vector3.Dot(Vector3.up, anchoredObject.transform.up)) >0.9f)
                    eulerAngles.y = 0;
                else if (Mathf.Abs(Vector3.Dot(Vector3.up, anchoredObject.transform.right)) >0.9f)
                    eulerAngles.x = 0;
                else
                    eulerAngles.z = 0;
                */
                print(eulerAngles.x + " ** " + eulerAngles.y + " ** " + eulerAngles.z);

                // Calculates the minimum rotation for the cube to be properly alligned
                var dif0 = Mathf.Abs(0 - eulerAngles.y);
                var dif90 = Mathf.Abs(90 - eulerAngles.y);
                var dif180 = Mathf.Abs(180 - eulerAngles.y);
                var dif270 = Mathf.Abs(270 - eulerAngles.y);
                var dif360 = Mathf.Abs(360 - eulerAngles.y);
                var minY = Mathf.Min(dif0, dif90, dif180, dif270, dif360);

                if (minY == dif0)
                    eulerAngles.y = 0;
                else if (minY == dif90)
                    eulerAngles.y = 90;
                else if (minY == dif180)
                    eulerAngles.y = 180;
                else if (minY == dif270)
                    eulerAngles.y = 270;
                else eulerAngles.y = 360;
                anchoredObject.transform.rotation = Quaternion.Euler(eulerAngles);

            }
        }
    }
    public void releaseRigidBody(string commanderName, string gameObjectName)
    {
        if (this.gameObject.name.Equals(commanderName)) return;
        if ((rigidbody != null) && (rigidbody.gameObject.name.Equals(gameObjectName)))
        {
            rigidbody = null;
            anchoredObject = null;
            released = true;
            print("Releasing. Commander= " + commanderName + ", gameObjectName= " + gameObjectName);
        }
    }
    public bool IsNotReleased(string objectName)
    {
        if (rigidbody != null)
            return ((!released) && (rigidbody.gameObject.name.Equals(objectName)));
        return false;
    }
}
