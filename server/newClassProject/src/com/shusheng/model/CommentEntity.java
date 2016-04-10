package com.shusheng.model;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.Id;
import javax.persistence.Table;

import org.hibernate.annotations.GenericGenerator;

@Entity
@Table(name = "commentEntity", schema = "")
public class CommentEntity {

	// id
	private String id;
	// comment评论
	private String comment;
	// 作业id
	private String homeworkId;
	// 评论人用户名
	private String username;
	// 评论人身份
	private int userStatus;
	// 评论时间
	private String commentTime;

	@Column(name = "comment", length = 100, nullable = true)
	public String getComment() {
		return comment;
	}

	@Column(name = "commentTime", length = 36, nullable = true)
	public String getCommentTime() {
		return commentTime;
	}

	@Column(name = "homeworkId", length = 36, nullable = true)
	public String getHomeworkId() {
		return homeworkId;
	}

	@Id
	@GeneratedValue(generator = "paymentableGenerator")
	@GenericGenerator(name = "paymentableGenerator", strategy = "uuid")
	public String getId() {
		return id;
	}

	@Column(name = "username", length = 20, nullable = true)
	public String getUsername() {
		return username;
	}

	@Column(name = "userStatus", length = 2, nullable = true)
	public int getUserStatus() {
		return userStatus;
	}

	public void setComment(String comment) {
		this.comment = comment;
	}

	public void setCommentTime(String commentTime) {
		this.commentTime = commentTime;
	}

	public void setHomeworkId(String homeworkId) {
		this.homeworkId = homeworkId;
	}

	public void setId(String id) {
		this.id = id;
	}

	public void setUsername(String username) {
		this.username = username;
	}

	public void setUserStatus(int userStatus) {
		this.userStatus = userStatus;
	}

}
