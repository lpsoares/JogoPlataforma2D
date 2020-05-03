using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Prime31;

public class PlayerController : MonoBehaviour
{
 
 	public CharacterController2D.CharacterCollisionState2D flags;
 	public float walkSpeed = 4.0f; // Depois de incluido, alterar no Unity Editor
 	public float jumpSpeed = 8.0f; // Depois de incluido, alterar no Unity Editor
	public float sinkSpeed = 4.0f; // Depois de incluido, alterar no Unity Editor
	public float doubleJumpSpeed = 6.0f; // Depois de incluido, alterar no Unity Editor
 	public float gravity = 9.8f;   // Depois de incluido, alterar no Unity Editor


 	public bool isGrounded;		// Se está no chão
 	public bool isJumping;		// Se está pulando
	public bool isFalling;		// Se estiver caindo
 	public bool isFacingRight;		// Se está olhando para a direita
	public bool doubleJumped; // informa se foi feito um pulo duplo

	public bool isDucking;
	public bool isClimbing;

	public AudioClip coin;


	public LayerMask mask;  // para filtrar os layers a serem analisados

 	private Vector3 moveDirection = Vector3.zero;			// direção que o personagem se move
 	private CharacterController2D characterController;	// Componente do Character Controller

	private BoxCollider2D boxCollider;
	private float colliderSizeY;
	private float colliderOffsetY;
	private Animator animator;

    void Start()
    {
    	characterController = GetComponent<CharacterController2D>(); // identifica o componente
		animator = GetComponent<Animator>();
		isFacingRight = true;
		boxCollider = GetComponent<BoxCollider2D>();
		colliderSizeY = boxCollider.size.y;
		colliderOffsetY = boxCollider.offset.y;
		
    }

    void Update()
    {

        moveDirection.x = Input.GetAxis("Horizontal"); // recupera valor dos controles
		moveDirection.x *= walkSpeed;

		// Conforme direção do personagem girar ele no eixo Y
		if(moveDirection.x < 0) {
			transform.eulerAngles = new Vector3(0,180,0);
			isFacingRight = false;
		}
		else if(moveDirection.x > 0)
		{
			transform.eulerAngles = new Vector3(0,0,0);
			isFacingRight = true;
		} // se direção em x == 0 mantenha como está a rotação

		if(isGrounded) {	// caso esteja no chão
			moveDirection.y = 0.0f;	// se no chão nem subir nem descer
			isJumping = false;
			doubleJumped = false; // se voltou ao chão pode faz pulo duplo
			if(Input.GetButton("Jump"))
			{
				moveDirection.y = jumpSpeed;
				isJumping = true;
			}
		} else {			// caso esteja pulando	
			if(Input.GetButtonUp("Jump") && moveDirection.y > 0) // Soltando botão diminui pulo
				moveDirection.y *= 0.5f;

			if(Input.GetButtonDown("Jump") && !doubleJumped) // Segundo clique faz pulo duplo
			{
				moveDirection.y = doubleJumpSpeed;
				doubleJumped = true;
			}

		}

		
		RaycastHit2D hit = Physics2D.Raycast(transform.position, -Vector2.up, 4f, mask);
		if (hit.collider != null && isGrounded) {
			transform.SetParent(hit.transform);
			if(Input.GetAxis("Vertical") < 0 && Input.GetButtonDown("Jump")) {
				moveDirection.y = -jumpSpeed;
				StartCoroutine(PassPlatform(hit.transform.gameObject));
			}
		} else {
			transform.SetParent(null);
		}

		if(moveDirection.y < 0)
			isFalling = true;
		else
			isFalling = false;

		moveDirection.y -= gravity * Time.deltaTime;				// aplica a gravidade

		animator.speed = 1.0f;
		if(isClimbing) {
			if(Input.GetAxis("Vertical") > 0) {
				moveDirection.y = walkSpeed;
				isJumping = true;
			}
			else if(Input.GetAxis("Vertical") < 0) {
				moveDirection.y = -walkSpeed;
			}
			else {
				if(!isGrounded) {
					moveDirection.y = 0.0f;
					animator.speed = 0.0f;
				}
			}
		}

		characterController.move(moveDirection * Time.deltaTime);	// move o personagem	

		flags = characterController.collisionState;					// recupera flags
		isGrounded = flags.below;									// define flag de chão

		if(Input.GetAxis("Vertical") < 0 && moveDirection.x == 0){
			if(!isDucking){
				boxCollider.size = new Vector2(boxCollider.size.x, 2*colliderSizeY/3);
				boxCollider.offset = new Vector2(boxCollider.offset.x, colliderOffsetY-colliderSizeY/6);
				characterController.recalculateDistanceBetweenRays();
			}
			isDucking = true;
		} else {
			if(isDucking){
				isDucking = false;
				boxCollider.size = new Vector2(boxCollider.size.x, colliderSizeY);
				boxCollider.offset = new Vector2(boxCollider.offset.x, colliderOffsetY);
				characterController.recalculateDistanceBetweenRays();
			}
		}

		UpdateAnimator();

    }

	IEnumerator PassPlatform(GameObject platform) {
		platform.GetComponent<EdgeCollider2D>().enabled = false;
		yield return new WaitForSeconds(1.0f);
		platform.GetComponent<EdgeCollider2D>().enabled = true;
	}

	private void UpdateAnimator() {
		// Atualizando Animator com estados do jogo
		animator.SetFloat("movementX", Mathf.Abs(moveDirection.x/walkSpeed));  // +Normalizado
		animator.SetFloat("movementY", moveDirection.y);
		animator.SetBool("isGrounded", isGrounded);
		animator.SetBool("isJumping", isJumping);
		animator.SetBool("isDucking", isDucking);
		animator.SetBool("isFalling", isFalling);
		animator.SetBool("isClimbing", isClimbing);
	}

	private void OnTriggerEnter2D(Collider2D other) {
		
		if(other.gameObject.layer == LayerMask.NameToLayer("Coins")) {
			AudioSource.PlayClipAtPoint(coin, this.gameObject.transform.position);
			Destroy(other.gameObject);
		}

		if(other.gameObject.layer == LayerMask.NameToLayer("LadderEffector")) {
			isClimbing = true;
		}

		if(other.gameObject.layer == LayerMask.NameToLayer("Water")) {
			animator.SetBool("isFalling", false);
			animator.SetTrigger("isSinking");
			StartCoroutine(Sink());
			enabled = false;
		}
	}

	IEnumerator Sink() {
		for(int i=0; i <= 20; i++) {
			characterController.move(new Vector3(0.0f, -sinkSpeed, 0.0f) * Time.deltaTime);
			yield return new WaitForSeconds(0.1f);
		}
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		
	}

	private void OnTriggerExit2D(Collider2D other) {
		
		if(other.gameObject.layer == LayerMask.NameToLayer("LadderEffector")) {
			isClimbing = false;
		}
	}



}
