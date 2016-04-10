package com.shusheng.model;

import javax.persistence.Transient;

public class User {
	
	//用户名
	@Transient
	private String username;
	//密码
	@Transient
	private String password;
	//用户身份
	@Transient
	private int userStatus;
	
	public String getUsername() {
		return username;
	}

	public void setUsername(String username) {
		this.username = username;
	}

	public String getPassword() {
		return password;
	}

	public void setPassword(String password) {
		this.password = password;
	}

	public int getUserStatus() {
		return userStatus;
	}

	public void setUserStatus(int userStatus) {
		this.userStatus = userStatus;
	}

}
