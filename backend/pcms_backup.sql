--
-- PostgreSQL database dump
--

\restrict CR7fYU7cZqAs5bU8U4itXQ4ihrhmcxgFiPLPyIv79sBQuouu1H5WZ9oHVyV5ql3

-- Dumped from database version 16.14 (Ubuntu 16.14-0ubuntu0.24.04.1)
-- Dumped by pg_dump version 16.14 (Ubuntu 16.14-0ubuntu0.24.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Roles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Roles" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL
);


--
-- Name: UsuarioRoles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UsuarioRoles" (
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL
);


--
-- Name: Usuarios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Usuarios" (
    "Id" uuid NOT NULL,
    "Nombre" text NOT NULL,
    "Apellido" text NOT NULL,
    "CorreoElectronico" text NOT NULL,
    "HashContrasena" text NOT NULL,
    "Activo" boolean NOT NULL,
    "FechaCreacion" timestamp with time zone NOT NULL
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Data for Name: Roles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Roles" ("Id", "Name") FROM stdin;
11111111-1111-1111-1111-111111111111	Admin
22222222-2222-2222-2222-222222222222	Usuario
33333333-3333-3333-3333-333333333333	Finanzas
44444444-4444-4444-4444-444444444444	Ventas
\.


--
-- Data for Name: UsuarioRoles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."UsuarioRoles" ("UserId", "RoleId") FROM stdin;
ed38c011-50f1-4cf1-8ea6-28bdc48cdf8b	11111111-1111-1111-1111-111111111111
77311a61-f7c6-4706-a990-a49ae208c477	22222222-2222-2222-2222-222222222222
\.


--
-- Data for Name: Usuarios; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Usuarios" ("Id", "Nombre", "Apellido", "CorreoElectronico", "HashContrasena", "Activo", "FechaCreacion") FROM stdin;
77311a61-f7c6-4706-a990-a49ae208c477	Bob	Jones	bob@test.com	AQAAAAIAAYagAAAAEL91NNhHRpbObQwefz3RWXNnaWMuZO00ocn8Iou1ddfaYE5N8mvSXvy7h7CZTbCLEw==	t	2026-07-23 14:04:03.995208-06
ed38c011-50f1-4cf1-8ea6-28bdc48cdf8b	John	Smith	john@test.com	AQAAAAIAAYagAAAAENS/9dPV84il3QtabtE9jGy0XpO06Nv/gbSxBnEZSWiY/RD9IgQzhg7pzpb0gN+lhg==	t	2026-07-23 13:54:20.670679-06
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260723194419_InitialCreate	10.0.10
20260723215048_AddUserRole	10.0.10
20260724204126_AddRolesAndUserRoles	10.0.10
20260724210112_UpdateRoleNames	10.0.10
20260724222541_LocalizeDatabaseSpanish	10.0.10
\.


--
-- Name: Roles PK_Roles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Roles"
    ADD CONSTRAINT "PK_Roles" PRIMARY KEY ("Id");


--
-- Name: UsuarioRoles PK_UsuarioRoles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UsuarioRoles"
    ADD CONSTRAINT "PK_UsuarioRoles" PRIMARY KEY ("UserId", "RoleId");


--
-- Name: Usuarios PK_Usuarios; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Usuarios"
    ADD CONSTRAINT "PK_Usuarios" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_UsuarioRoles_RoleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UsuarioRoles_RoleId" ON public."UsuarioRoles" USING btree ("RoleId");


--
-- Name: UsuarioRoles FK_UsuarioRoles_Roles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UsuarioRoles"
    ADD CONSTRAINT "FK_UsuarioRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."Roles"("Id") ON DELETE CASCADE;


--
-- Name: UsuarioRoles FK_UsuarioRoles_Usuarios_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UsuarioRoles"
    ADD CONSTRAINT "FK_UsuarioRoles_Usuarios_UserId" FOREIGN KEY ("UserId") REFERENCES public."Usuarios"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict CR7fYU7cZqAs5bU8U4itXQ4ihrhmcxgFiPLPyIv79sBQuouu1H5WZ9oHVyV5ql3

