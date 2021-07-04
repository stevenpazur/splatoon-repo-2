using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ToWall : MonoBehaviour
{
    public GameObject model, gun;
    public ParticleSystem swimParticle;
    public float Velocity, SwimVelocity;
    private float playerSpeed = 6; // move speed
    private float turnSpeed = 90; // turning speed (degrees/second)
    public float lerpSpeed = 10; // smoothing speed
    private float gravity = 0.15f; // gravity acceleration
    private bool isGrounded;
    public float deltaGround = -0.5f; // character is grounded up to this distance
    private float jumpSpeed = 10; // vertical jump initial speed
    public float jumpRange = 0.1f; // range to detect target wall
    public float jumpHeight = 0.5f;
    private bool jumpingUp = false;
    [Range(0, 1)] public float similarColorsOffsetValue = 0.1f;
    private Vector3 surfaceNormal; // current surface normal
    private Vector3 myNormal; // character normal
    private float distGround; // distance from character position to ground
    private bool jumping = false; // flag &quot;I'm jumping to wall&quot;
    private float vertSpeed = 0; // vertical jump current speed
    private bool blockRotation = false;

    private Transform myTransform;
    //public BoxCollider boxCollider; // drag BoxCollider ref in editor
    public Animator anim;
    public Color theColor;

    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;
    [Range(0, 1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    private void Start()
    {
        myNormal = transform.up; // normal starts as character up direction
        myTransform = transform;
        //GetComponent<Rigidbody>().freezeRotation = true; // disable physics rotation
                                         // distance from transform.position to ground
        distGround = GetComponent<CharacterController>().bounds.size.y - GetComponent<CharacterController>().center.y;
    }

    private void FixedUpdate()
    {
        // apply constant weight force according to character normal:
        //GetComponent<Rigidbody>().AddForce(-gravity * GetComponent<Rigidbody>().mass * myNormal);
        if (!isGrounded)
        {
            print("pull");
            GetComponent<CharacterController>().Move(-gravity * myNormal);
        }
    }

    private void Update()
    {
        // jump code - jump to wall or simple jump
        if (jumping) return; // abort Update while jumping to a wall

        anim.SetBool("shooting", blockRotation);

        Ray ray;
        RaycastHit hit;


        if (Input.GetButton("Fire2"))
        {
            GetComponent<ShootingSystem>().canShoot = false;


            //Vector3 rayOffset = new Vector3(transform.up.x, transform.up.y + 0.1f, transform.up.z);
            RaycastHit raycastHit;
            ray = new Ray(transform.position, -transform.up);
            Debug.DrawRay(transform.position, -transform.up, Color.red, 2f);
            if (Physics.Raycast(ray, out raycastHit))
            {
                if (raycastHit.transform.tag != "Paintable")
                {
                    print("not paintable");
                    return;
                }

                Color color = new Color();

                //grabbedTexture = true;
                Material mat = raycastHit.collider.gameObject.GetComponent<Renderer>().sharedMaterial;
                Shader shader = mat.shader;
                string shaderName = ShaderUtil.GetPropertyName(shader, 0);
                Texture texture = mat.GetTexture(shaderName);
                Texture2D t2d = TextureToTexture2D(texture);
                Vector2 pCoord = raycastHit.textureCoord;
                pCoord.x *= t2d.width;
                pCoord.y *= t2d.height;
                Vector2 tiling = mat.GetTextureScale(shaderName);
                int textureAtX = Mathf.FloorToInt(pCoord.x * tiling.x);
                int textureAtZ = Mathf.FloorToInt(pCoord.y * tiling.y);
                Color cool = t2d.GetPixel(textureAtX, textureAtZ);
                color = cool;

                Color color1 = new Color(theColor.r * (1f - similarColorsOffsetValue), theColor.g * (1f - similarColorsOffsetValue), theColor.b * (1f - similarColorsOffsetValue), theColor.a * (1f - similarColorsOffsetValue));
                Color color2 = new Color(theColor.r * (1f + similarColorsOffsetValue), theColor.g * (1f + similarColorsOffsetValue), theColor.b * (1f + similarColorsOffsetValue), theColor.a * (1f + similarColorsOffsetValue));
                if (ColorsSimilar(color1, color2, color))
                {
                    swimParticle.Play();
                    model.SetActive(false);
                    gun.SetActive(false);
                    playerSpeed = SwimVelocity;
                }
                else
                {
                    swimParticle.Stop();
                    model.SetActive(true);
                    gun.SetActive(true);
                    playerSpeed = Velocity;
                    myNormal = Vector3.up;
                }

            }
        }
        else
        {
            swimParticle.Stop();
            model.SetActive(true);
            gun.SetActive(true);
            playerSpeed = Velocity;
            myNormal = Vector3.up;
        }
        ray = new Ray(myTransform.position, myTransform.forward);

        if (Physics.Raycast(ray, out hit, jumpRange) && Input.GetButton("Fire2"))
        { // wall ahead?
            JumpToWall(hit.point, hit.normal); // yes: jump to the wall
        }
        else if (!Input.GetButton("Fire2"))
        {
            myNormal = Vector3.up;
            GetComponent<ShootingSystem>().canShoot = true;
        }

        // movement code:

        float InputX = Input.GetAxis("Horizontal");
        float InputZ = Input.GetAxis("Vertical");
        float inputMagnitude = new Vector2(InputX, InputZ).sqrMagnitude;

        if(inputMagnitude > 0.1f)
        {
            anim.SetFloat("Blend", inputMagnitude, StartAnimTime, Time.deltaTime);
            anim.SetFloat("X", InputX, StartAnimTime / 3, Time.deltaTime);
            anim.SetFloat("Y", InputZ, StartAnimTime / 3, Time.deltaTime);
        }
        else
        {
            anim.SetFloat("Blend", inputMagnitude, StopAnimTime, Time.deltaTime);
            anim.SetFloat("X", InputX, StopAnimTime / 3, Time.deltaTime);
            anim.SetFloat("Y", InputZ, StopAnimTime / 3, Time.deltaTime);
        }

        var cam = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.Normalize();
        right.Normalize();

        Vector3 desiredMoveDirection = forward * InputZ + right * InputX;

        //Camera
        if (desiredMoveDirection != Vector3.zero && !blockRotation)
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, Quaternion.LookRotation(desiredMoveDirection), 0.1f);
        GetComponent<CharacterController>().Move(desiredMoveDirection * Time.deltaTime * playerSpeed);

        if (Input.GetButton("Fire1"))
        {
            blockRotation = true;
            RotateToCamera(cam, desiredMoveDirection, 0.1f, transform);
        }
        else
        {
            blockRotation = false;
        }

        // update surface normal and isGrounded:
        ray = new Ray(myTransform.position, -myNormal); // cast ray downwards
        if (Physics.Raycast(ray, out hit))
        { // use it to update myNormal and isGrounded
            isGrounded = hit.distance <= distGround + deltaGround;
            if (isGrounded)
                jumpingUp = false;
            surfaceNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            // assume usual ground normal to avoid "falling forever"
            surfaceNormal = Vector3.up;
        }
        myNormal = Vector3.Lerp(myNormal, surfaceNormal, lerpSpeed * Time.deltaTime);
        // find forward direction with new myNormal:
        Vector3 myForward = Vector3.Cross(myTransform.right, myNormal);
        // align character to the new myNormal while keeping the forward direction:
        Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
        myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, lerpSpeed * Time.deltaTime);
        // move the character forth/back with Vertical axis:
        //myTransform.Translate(0, 0, Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime);
        if (myNormal != Vector3.up)
        {
            if(Input.GetAxis("Vertical") > 0)
                GetComponent<CharacterController>().Move(myTransform.forward * Input.GetAxis("Vertical") * playerSpeed * Time.deltaTime);
            if(Input.GetAxis("Vertical") < 0)
                GetComponent<CharacterController>().Move(-myTransform.forward * Input.GetAxis("Vertical") * playerSpeed * Time.deltaTime);
        }
    }

    private void RotateToCamera(Camera cam, Vector3 desiredMoveDirection, float desiredRotationSpeed, Transform t)
    {
        var forward = cam.transform.forward;

        desiredMoveDirection = forward;
        Quaternion lookAtRotation = Quaternion.LookRotation(desiredMoveDirection);
        Quaternion lookAtRotationOnly_Y = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        t.rotation = Quaternion.Slerp(transform.rotation, lookAtRotationOnly_Y, desiredRotationSpeed);
    }

    private bool ColorsSimilar(Color color1, Color color2, Color checkedColor)
    {
        bool isRedGood = checkedColor.r >= Mathf.Min(color1.r, color2.r) && checkedColor.r <= Mathf.Max(color1.r, color2.r);

        bool isGreenGood = checkedColor.g >= Mathf.Min(color1.g, color2.g) && checkedColor.g <= Mathf.Max(color1.g, color2.g);

        bool isBlueGood = checkedColor.b >= Mathf.Min(color1.b, color2.b) && checkedColor.b <= Mathf.Max(color1.b, color2.b);

        bool isAlphaGood = checkedColor.a >= Mathf.Min(color1.a, color2.a) && checkedColor.a <= Mathf.Max(color1.a, color2.a);// May not need this one if you don't have an alpha channel :P

        return isRedGood && isGreenGood && isBlueGood && isAlphaGood;
    }

    private Texture2D TextureToTexture2D(Texture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        Graphics.Blit(texture, renderTexture);

        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);
        return texture2D;
    }

    private void JumpToWall(Vector3 point, Vector3 normal)
    {
        // jump to wall
        jumping = true; // signal it's jumping to wall
        Vector3 orgPos = myTransform.position;
        Quaternion orgRot = myTransform.rotation;
        Vector3 dstPos = point + normal * (distGround + 0.5f); // will jump to 0.5 above wall
        Vector3 myForward = Vector3.Cross(myTransform.right, normal);
        Quaternion dstRot = Quaternion.LookRotation(myForward, normal);

        StartCoroutine(jumpTime(orgPos, orgRot, dstPos, dstRot, normal));
        //jumptime
    }

    private IEnumerator jumpTime(Vector3 orgPos, Quaternion orgRot, Vector3 dstPos, Quaternion dstRot, Vector3 normal)
    {
        //for (float t = 0.0f; t < 0.2f;)
        //{
        //    t += Time.deltaTime;
        myTransform.position = Vector3.Lerp(myTransform.position, dstPos, 0.02f);
        //myTransform.position = dstPos;
        myTransform.rotation = Quaternion.Slerp(myTransform.rotation, dstRot, 0.02f);
        //myTransform.rotation = dstRot;
        yield return null; // return here next frame
        //}
        myNormal = normal; // update myNormal
        jumping = false; // jumping to wall finished

    }

}
