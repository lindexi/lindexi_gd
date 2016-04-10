package com.shusheng.model;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.Id;
import javax.persistence.Table;

import org.hibernate.annotations.GenericGenerator;

@Entity
@Table(name = "teacher", schema = "")
public class Teacher extends User{

	// id
	private String id;
	// 用户名
	private String username;
	// 密码
	private String password;
	// 邮箱
	private String email;
	// 教工号
	private String teaNum;
	// qq号
	private String qqNum;
	// 真实姓名
	private String realName;
	// 手机号
	private String telephone;

	@Column(name = "email", nullable = true, length = 20)
	public String getEmail() {
		return email;
	}

	@Id
	@GeneratedValue(generator = "paymentableGenerator")
	@GenericGenerator(name = "paymentableGenerator", strategy = "uuid")
	@Column(name = "ID", nullable = true, length = 36)
	public String getId() {
		return id;
	}

	@Column(name = "password", nullable = true, length = 20)
	public String getPassword() {
		return password;
	}

//	@Column(name = "pubHomeworkId", nullable = false, length = 20)
//	public String getPubHomeworkId() {
//		return pubHomeworkId;
//	}

	@Column(name = "qqNum", nullable = true, length = 20)
	public String getQqNum() {
		return qqNum;
	}

	@Column(name = "realName", nullable = true, length = 20)
	public String getRealName() {
		return realName;
	}

	@Column(name = "teaNum", nullable = true, length = 20)
	public String getTeaNum() {
		return teaNum;
	}

	@Column(name = "telephone", nullable = true, length = 20)
	public String getTelephone() {
		return telephone;
	}

	@Column(name = "username", nullable = true, length = 20)
	public String getUsername() {
		return username;
	}

	public void setEmail(String email) {
		this.email = email;
	}

	public void setId(String id) {
		this.id = id;
	}

	public void setPassword(String password) {
		super.setPassword(password);
		this.password = password;
	}

//	public void setPubHomeworkId(String pubHomeworkId) {
//		this.pubHomeworkId = pubHomeworkId;
//	}

	public void setQqNum(String qqNum) {
		this.qqNum = qqNum;
	}

	public void setRealName(String realName) {
		this.realName = realName;
	}

	public void setTeaNum(String teaNum) {
		this.teaNum = teaNum;
	}

	public void setTelephone(String telephone) {
		this.telephone = telephone;
	}

	public void setUsername(String username) {
		super.setUsername(username);//用户名
		super.setUserStatus(1);//教师
		this.username = username;
	}

}
