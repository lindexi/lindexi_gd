package com.shusheng.model;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.Id;
import javax.persistence.Table;

import org.hibernate.annotations.GenericGenerator;

@Entity
@Table(name = "homework", schema = "")
public class Homework {

	// id
	private String id;
	// 发布的科目
	private String subject;
	// 作业内容
	private String content;
	// 发布时间
	private String publishTime;
	// 作业提交时间
	private String endTime;
	// 发布人
	private String publishUsername;
	// 发布人身份
	private int userStatus;
	// 具体时间
	private String specificTime;
	// 班级名称,主要用于班级公告
	private String classStr;

	@Column(name = "classStr", length = 100, nullable = true)
	public String getClassStr() {
		return classStr;
	}

	@Column(name = "content", length = 20, nullable = true)
	public String getContent() {
		return content;
	}

	@Column(name = "endTime", length = 20, nullable = true)
	public String getEndTime() {
		return endTime;
	}

	@Id
	@GeneratedValue(generator = "paymentableGenerator")
	@GenericGenerator(name = "paymentableGenerator", strategy = "uuid")
	public String getId() {
		return id;
	}

	@Column(name = "publishTime", length = 20, nullable = true)
	public String getPublishTime() {
		return publishTime;
	}

	@Column(name = "publishUsername", length = 20, nullable = true)
	public String getPublishUsername() {
		return publishUsername;
	}

	@Column(name = "specificTime", length = 20, nullable = true)
	public String getSpecificTime() {
		return specificTime;
	}

	@Column(name = "subject", length = 20, nullable = true)
	public String getSubject() {
		return subject;
	}

	@Column(name = "userStatus", length = 2, nullable = true)
	public int getUserStatus() {
		return userStatus;
	}

	public void setClassStr(String classStr) {
		this.classStr = classStr;
	}

	public void setContent(String content) {
		this.content = content;
	}

	public void setEndTime(String endTime) {
		this.endTime = endTime;
	}

	public void setId(String id) {
		this.id = id;
	}

	public void setPublishTime(String publishTime) {
		this.publishTime = publishTime;
	}

	public void setPublishUsername(String publishUsername) {
		this.publishUsername = publishUsername;
	}

	public void setSpecificTime(String specificTime) {
		this.specificTime = specificTime;
	}

	public void setSubject(String subject) {
		this.subject = subject;
	}

	public void setUserStatus(int userStatus) {
		this.userStatus = userStatus;
	}

}
