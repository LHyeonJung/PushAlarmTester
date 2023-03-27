# PushAlarmTester
UDP를 통해 push 알람을 보내기 위한 테스트 툴

    UDP란?(User Datagram Protocol)
        - UDP(User Datagram Protocol)는 비연결형, 신뢰성이 없는 전송 프로토콜이다.
        - IP데이터그램을 캡슐화하여 보내는 방법과 연결 설정을 하지 않고 보내는 방법을 제공한다.
        - UDP는 TCP/IP 5계층에서 Transport Layer(전송계층)의 프로토콜이다.
        
    UDP의 특징
        - UDP는 흐름제어, 오류제어 또는 손상된 세그먼트의 수신에 대한 재전송을 하지 않는다. 
             (따라서 내용이 전송 중에 손실 될 수 있고, 전송되는 세그먼트의 순서가 바뀔 수 있다.)
        - UDP는 TCP보다 간단하고 빠르다.
        - 작은 header size를 가지고 있다.
        - 흐름제어를 하지 않기 때문에 전송 속도를 최대한 빠르게 할 수 있다.
        - 수신자와 송신자 간의 handshaking이 없는 connectionless 성질을 가진다.
        
    UDP를 사용하는 이유
        - UDP는 왜 사용할까? 
        - UDP는 TCP와 다르게 흐름제어나 오류제어 등이 없기 때문에 전송 속도를 최대한 빠르게 할 수 있다. 
        - 하지만 TCP처럼 신뢰성 있는 전송을 보장할 수 없다. 따라서 신뢰성보다 속도가 중요한 부문에서 UDP를 사용하게 된다. 
            (예를 들어 유튜브 동영상 같은 스트리밍 어플리케이션/DNS/SNMP는 신뢰성보다 속도가 중요하므로 UDP를 사용한다.)



---

### 목적
* 특정 제품이 보내주는 알림을 관리도구에서 확인하기 위함

---

### 구성도
![구성도](https://user-images.githubusercontent.com/28182969/227831945-dc38fa4a-c7ac-4b14-a4e5-f6db66440c0e.JPG)

---

### 프로세스   
![process](https://user-images.githubusercontent.com/28182969/227832143-a0a9d63d-c8ad-40c7-b33d-d7a8814000c8.JPG)

---

### 프로토콜 구상안
```javascript
/*
   date : 로그 발생 시각
   component : 제품 유형
   id : 등록된 제품의 id
   msg : 요약 메시지
   detailmsg : 상세 메시지
*/

{
	"instance":[
		{
		"date":"2021-04-11 12:14:74",
		"component":“PRODUCT_A",
		"id":"test_A",
		"msg":"test msg",
		"detailmsg":"test detail msg"
		},
		{
		"date":"2021-04-11 12:14:74",
		"component":" PRODUCT_B",
		"id":"test_B",
		"msg":"test msg",
		"detailmsg":"test detail msg"
		}
	]
}
```

---

### 설정 조건 및 프로토콜 항목
![setting](https://user-images.githubusercontent.com/28182969/227833251-ffb55f30-8e7e-4b29-af4a-f1da7fa98caa.JPG)

