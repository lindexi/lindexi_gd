package com.shusheng.model;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.Id;
import javax.persistence.Table;

import org.hibernate.annotations.GenericGenerator;

@Entity
@Table(name = "student", schema = "")
public class Student extends User{
	// id
	private String id;
	// 用户名
	private String username;
	// 密码
	private String password;
	// 邮箱
	private String email;
	// 学号
	private String stuNum;
	// 年纪
	private String grade;
	// 班级
	private String classStr;
	// 真实姓名
	private String realName;
	// 手机号
	private String telephone;
	// qq号
	private String qqNum;

	@Column(name = "classStr", nullable = true, length = 36)
	public String getClassStr() {
		return classStr;
	}
	@Column(name = "email", nullable = true, length = 36)
	public String getEmail() {
		return email;
	}
	@Column(name = "grade", nullable = true, length = 36)
	public String getGrade() {
		return grade;
	}

	@Id
	@GeneratedValue(generator = "paymentableGenerator")
	@GenericGenerator(name = "paymentableGenerator", strategy = "uuid")
	@Column(name = "ID", nullable = true, length = 36)
	public String getId() {
		return id;
	}
	
	@Column(name = "password", nullable = true, length = 36)
	public String getPassword() {
		return password;
	}
//
//	@Column(name = "pubHomeworkId", nullable = false, length = 36)
//	public String getPubHomeworkId() {
//		return pubHomeworkId;
//	}

	@Column(name = "qqNum", nullable = true, length = 36)
	public String getQqNum() {
		return qqNum;
	}

	@Column(name = "realName", nullable = true, length = 36)
	public String getRealName() {
		return realName;
	}

	@Column(name = "stuNum", nullable = true, length = 36)
	public String getStuNum() {
		return stuNum;
	}

	@Column(name = "telephone", nullable = true, length = 36)
	public String getTelephone() {
		return telephone;
	}

	@Column(name = "username", nullable = true, length = 36)
	public String getUsername() {
		return username;
	}

	public void setClassStr(String classStr) {
		this.classStr = classStr;
	}

	public void setEmail(String email) {
		this.email = email;
	}

	public void setGrade(String grade) {
		this.grade = grade;
	}

	public void setId(String id) {
		this.id = id;
	}

	public void setPassword(String password) {
		super.setPassword(password);
		this.password = password;
	}
//
//	public void setPubHomeworkId(String pubHomeworkId) {
//		this.pubHomeworkId = pubHomeworkId;
//	}

	public void setQqNum(String qqNum) {
		this.qqNum = qqNum;
	}

	public void setRealName(String realName) {
		this.realName = realName;
	}

	public void setStuNum(String stuNum) {
		this.stuNum = stuNum;
	}

	public void setTelephone(String telephone) {
		this.telephone = telephone;
	}

	public void setUsername(String username) {
		super.setUsername(username);
		super.setUserStatus(0);
		this.username = username;
	}
}
