--
-- PostgreSQL database dump
--

\restrict t3yB2rlHng9QciH3JHn2769PpTRTUPn68kQkqlsC4qMdo6IWE4aLXW7SLUl1AsU

-- Dumped from database version 17.6
-- Dumped by pg_dump version 17.6

-- Started on 2026-02-17 21:21:28

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 224 (class 1259 OID 17354)
-- Name: bookings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.bookings (
    id integer NOT NULL,
    user_id integer,
    tour_id integer,
    guests_count integer NOT NULL,
    total_price numeric(10,2) NOT NULL,
    status character varying(20) DEFAULT 'pending'::character varying,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.bookings OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 17353)
-- Name: bookings_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.bookings_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.bookings_id_seq OWNER TO postgres;

--
-- TOC entry 4852 (class 0 OID 0)
-- Dependencies: 223
-- Name: bookings_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.bookings_id_seq OWNED BY public.bookings.id;


--
-- TOC entry 218 (class 1259 OID 17302)
-- Name: countries; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.countries (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    code character varying(2) NOT NULL,
    flag_emoji character varying(10)
);


ALTER TABLE public.countries OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 17301)
-- Name: countries_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.countries_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.countries_id_seq OWNER TO postgres;

--
-- TOC entry 4853 (class 0 OID 0)
-- Dependencies: 217
-- Name: countries_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.countries_id_seq OWNED BY public.countries.id;


--
-- TOC entry 220 (class 1259 OID 17311)
-- Name: hotels; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.hotels (
    id integer NOT NULL,
    name character varying(200) NOT NULL,
    country_id integer,
    city character varying(100) NOT NULL,
    stars integer,
    rating numeric(2,1),
    image_url character varying(500),
    description text,
    CONSTRAINT hotels_rating_check CHECK (((rating >= (0)::numeric) AND (rating <= (5)::numeric))),
    CONSTRAINT hotels_stars_check CHECK (((stars >= 1) AND (stars <= 5)))
);


ALTER TABLE public.hotels OWNER TO postgres;

--
-- TOC entry 219 (class 1259 OID 17310)
-- Name: hotels_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.hotels_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.hotels_id_seq OWNER TO postgres;

--
-- TOC entry 4854 (class 0 OID 0)
-- Dependencies: 219
-- Name: hotels_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.hotels_id_seq OWNED BY public.hotels.id;


--
-- TOC entry 226 (class 1259 OID 17373)
-- Name: tours; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tours (
    id integer NOT NULL,
    title character varying(255) NOT NULL,
    description text,
    image_url text NOT NULL,
    departure_date date NOT NULL,
    nights integer NOT NULL,
    price numeric(10,2) NOT NULL,
    rating integer,
    location character varying(255) NOT NULL,
    duration integer,
    country character varying(100),
    is_active boolean DEFAULT true,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT tours_rating_check CHECK (((rating >= 1) AND (rating <= 5)))
);


ALTER TABLE public.tours OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 17372)
-- Name: tours_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.tours_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.tours_id_seq OWNER TO postgres;

--
-- TOC entry 4855 (class 0 OID 0)
-- Dependencies: 225
-- Name: tours_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.tours_id_seq OWNED BY public.tours.id;


--
-- TOC entry 222 (class 1259 OID 17342)
-- Name: users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.users (
    id integer NOT NULL,
    username character varying(50) NOT NULL,
    email character varying(100) NOT NULL,
    password_hash character varying(255) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    last_login timestamp without time zone
);


ALTER TABLE public.users OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 17341)
-- Name: users_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.users_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.users_id_seq OWNER TO postgres;

--
-- TOC entry 4856 (class 0 OID 0)
-- Dependencies: 221
-- Name: users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.users_id_seq OWNED BY public.users.id;


--
-- TOC entry 4665 (class 2604 OID 17357)
-- Name: bookings id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings ALTER COLUMN id SET DEFAULT nextval('public.bookings_id_seq'::regclass);


--
-- TOC entry 4661 (class 2604 OID 17305)
-- Name: countries id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.countries ALTER COLUMN id SET DEFAULT nextval('public.countries_id_seq'::regclass);


--
-- TOC entry 4662 (class 2604 OID 17314)
-- Name: hotels id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.hotels ALTER COLUMN id SET DEFAULT nextval('public.hotels_id_seq'::regclass);


--
-- TOC entry 4668 (class 2604 OID 17376)
-- Name: tours id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tours ALTER COLUMN id SET DEFAULT nextval('public.tours_id_seq'::regclass);


--
-- TOC entry 4663 (class 2604 OID 17345)
-- Name: users id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users ALTER COLUMN id SET DEFAULT nextval('public.users_id_seq'::regclass);


--
-- TOC entry 4844 (class 0 OID 17354)
-- Dependencies: 224
-- Data for Name: bookings; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.bookings (id, user_id, tour_id, guests_count, total_price, status, created_at) FROM stdin;
\.


--
-- TOC entry 4838 (class 0 OID 17302)
-- Dependencies: 218
-- Data for Name: countries; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.countries (id, name, code, flag_emoji) FROM stdin;
1	Турция	TR	🇹🇷
2	Египет	EG	🇪🇬
3	ОАЭ	AE	🇦🇪
4	Таиланд	TH	🇹🇭
5	Индия	IN	🇮🇳
\.


--
-- TOC entry 4840 (class 0 OID 17311)
-- Dependencies: 220
-- Data for Name: hotels; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.hotels (id, name, country_id, city, stars, rating, image_url, description) FROM stdin;
1	Rixos Radamis Blue Planet	1	Шарм-эль-Шейх	5	4.8	/images/banner_aquamariner-resort-by-swandor_B2C.jpg	Роскошный отель на берегу Красного моря
2	Aquamariner Resort	1	Мармарис	5	4.7	/images/hotels-only_B2C.jpg	Современный курорт с аквапарком
3	Cleopatra Luxury Resort	2	Хургада	5	4.6	/images/banner_aquamariner-resort-by-swandor_B2C.jpg	Отель на первой линии пляжа
4	Beach Paradise Hotel	2	Шарм-эль-Шейх	4	4.5	/images/hotels-only_B2C.jpg	Уютный семейный отель
5	Emirates Palace	3	Дубай	5	4.9	/images/banner_aquamariner-resort-by-swandor_B2C.jpg	Роскошный отель в центре Дубая
\.


--
-- TOC entry 4846 (class 0 OID 17373)
-- Dependencies: 226
-- Data for Name: tours; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tours (id, title, description, image_url, departure_date, nights, price, rating, location, duration, country, is_active, created_at) FROM stdin;
1	Rixos Radamis Blue Planet	\N	https://pic-h.cdn.pegast.ru/getimage-h/thumbh338/b7/fa/45/275a06f48882987d42caca6b32dd1948de5ea94ca5f24238a6d8d969d8/679721fa7aa27.jpg	2025-01-18	7	181544.00	5	Египет, Шарм-Эль-Шейх	7	Египет	t	2025-12-27 09:26:07.753281
2	Paradise Beach Resort	\N	https://via.placeholder.com/338x230/4A90E2/ffffff?text=Paradise+Beach	2025-02-10	10	256000.00	4	Мальдивы, Мале	10	Мальдивы	t	2025-12-27 09:26:07.753281
3	Grand Hotel Vienna	\N	https://via.placeholder.com/338x230/E67E22/ffffff?text=Grand+Hotel	2025-03-05	5	95000.00	5	Австрия, Вена	5	Австрия	t	2025-12-27 09:26:07.753281
4	Tropical Paradise	\N	https://via.placeholder.com/338x230/27AE60/ffffff?text=Tropical	2025-01-25	14	320000.00	5	Таиланд, Пхукет	14	Таиланд	t	2025-12-27 09:26:07.753281
5	Mountain Resort	\N	https://via.placeholder.com/338x230/8E44AD/ffffff?text=Mountain	2025-02-15	7	125000.00	4	Швейцария, Цюрих	7	Швейцария	t	2025-12-27 09:26:07.753281
6	Beach Hotel Antalya	\N	https://via.placeholder.com/338x230/E74C3C/ffffff?text=Antalya	2025-01-20	10	89000.00	4	Турция, Анталия	10	Турция	t	2025-12-27 09:26:07.753281
7	Luxury Resort Dubai	\N	https://via.placeholder.com/338x230/3498DB/ffffff?text=Dubai	2025-02-01	12	450000.00	5	ОАЭ, Дубай	12	ОАЭ	t	2025-12-27 09:26:07.753281
8	Santorini Dreams	\N	https://via.placeholder.com/338x230/9B59B6/ffffff?text=Santorini	2025-03-10	8	189000.00	5	Греция, Санторини	8	Греция	t	2025-12-27 09:26:07.753281
9	Bali Paradise	\N	https://via.placeholder.com/338x230/1ABC9C/ffffff?text=Bali	2025-01-28	11	278000.00	4	Индонезия, Бали	11	Индонезия	t	2025-12-27 09:26:07.753281
\.


--
-- TOC entry 4842 (class 0 OID 17342)
-- Dependencies: 222
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.users (id, username, email, password_hash, created_at, last_login) FROM stdin;
\.


--
-- TOC entry 4857 (class 0 OID 0)
-- Dependencies: 223
-- Name: bookings_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.bookings_id_seq', 1, false);


--
-- TOC entry 4858 (class 0 OID 0)
-- Dependencies: 217
-- Name: countries_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.countries_id_seq', 5, true);


--
-- TOC entry 4859 (class 0 OID 0)
-- Dependencies: 219
-- Name: hotels_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.hotels_id_seq', 5, true);


--
-- TOC entry 4860 (class 0 OID 0)
-- Dependencies: 225
-- Name: tours_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tours_id_seq', 9, true);


--
-- TOC entry 4861 (class 0 OID 0)
-- Dependencies: 221
-- Name: users_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.users_id_seq', 1, false);


--
-- TOC entry 4687 (class 2606 OID 17361)
-- Name: bookings bookings_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings
    ADD CONSTRAINT bookings_pkey PRIMARY KEY (id);


--
-- TOC entry 4675 (class 2606 OID 17309)
-- Name: countries countries_code_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.countries
    ADD CONSTRAINT countries_code_key UNIQUE (code);


--
-- TOC entry 4677 (class 2606 OID 17307)
-- Name: countries countries_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.countries
    ADD CONSTRAINT countries_pkey PRIMARY KEY (id);


--
-- TOC entry 4679 (class 2606 OID 17320)
-- Name: hotels hotels_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.hotels
    ADD CONSTRAINT hotels_pkey PRIMARY KEY (id);


--
-- TOC entry 4689 (class 2606 OID 17383)
-- Name: tours tours_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tours
    ADD CONSTRAINT tours_pkey PRIMARY KEY (id);


--
-- TOC entry 4681 (class 2606 OID 17352)
-- Name: users users_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_email_key UNIQUE (email);


--
-- TOC entry 4683 (class 2606 OID 17348)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- TOC entry 4685 (class 2606 OID 17350)
-- Name: users users_username_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_username_key UNIQUE (username);


--
-- TOC entry 4691 (class 2606 OID 17362)
-- Name: bookings bookings_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings
    ADD CONSTRAINT bookings_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- TOC entry 4690 (class 2606 OID 17321)
-- Name: hotels hotels_country_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.hotels
    ADD CONSTRAINT hotels_country_id_fkey FOREIGN KEY (country_id) REFERENCES public.countries(id);


-- Completed on 2026-02-17 21:21:28

--
-- PostgreSQL database dump complete
--

\unrestrict t3yB2rlHng9QciH3JHn2769PpTRTUPn68kQkqlsC4qMdo6IWE4aLXW7SLUl1AsU

