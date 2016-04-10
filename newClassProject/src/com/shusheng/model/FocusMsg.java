package com.shusheng.model;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.Id;
import javax.persistence.Table;

import org.hibernate.annotations.GenericGenerator;

@Entity
@Table(name = "focusMsg", schema = "")
public class FocusMsg {

	// id
	private String id;
	// 关注人的姓名
	private String username;
	// 关注人的身份
	private int userStatus;
	// 关注科目
	private String subjectName;
	// 被关注人身份(0表示学生，1 表示教师)
	private int focusPStatus;
	// 被关注人姓名
	private String focusedName;

	@Column(name = "focusedName", length = 20, nullable = true)
	public String getFocusedName() {
		return focusedName;
	}

	@Column(name = "focusPStatus", length = 20, nullable = true)
	public int getFocusPStatus() {
		return focusPStatus;
	}

	@Id
	@GeneratedValue(generator = "paymentableGenerator")
	@GenericGenerator(name = "paymentableGenerator", strategy = "uuid")
	public String getId() {
		return id;
	}

	@Column(name = "subjectName", length = 20, nullable = true)
	public String getSubjectName() {
		return subjectName;
	}
	
	@Column(name = "username", length = 20, nullable = true)
	public String getUsername() {
		return username;
	}

	@Column(name = "userStatus", length = 2, nullable = true)
	public int getUserStatus() {
		return userStatus;
	}

	public void setFocusedName(String focusedName) {
		this.focusedName = focusedName;
	}

	public void setFocusPStatus(int focusPStatus) {
		this.focusPStatus = focusPStatus;
	}

	public void setId(String id) {
		this.id = id;
	}

	public void setSubjectName(String subjectName) {
		this.subjectName = subjectName;
	}

	public void setUsername(String username) {
		this.username = username;
	}

	public void setUserStatus(int userStatus) {
		this.userStatus = userStatus;
	}

}
